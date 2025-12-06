using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NeonSDK
{
    public sealed class NeonClient : IDisposable
    {
        private readonly HttpClient _http;
        private readonly bool _ownsHttpClient;
        private readonly INeonAudioPlayer? _audioPlayer;
        private readonly Uri _baseUri;

        public NeonEvents Events { get; } = new();

        public NeonClient(
            INeonAudioPlayer? audioPlayer,
            HttpClient? httpClient = null,
            string? baseUrl = null)
        {
            _audioPlayer = audioPlayer;
            _ownsHttpClient = httpClient is null;
            _http = httpClient ?? new HttpClient();

            baseUrl ??= Environment.GetEnvironmentVariable("NEON_GATEWAY_URL") ?? "http://localhost:8000";
            _baseUri = new Uri(baseUrl.TrimEnd('/'));
        }

        public void Dispose()
        {
            if (_ownsHttpClient)
                _http.Dispose();
        }

        // ====================================================================
        // PUBLIC — TEXT INPUT
        // ====================================================================
        public async Task SendTextAsync(
            string text,
            bool speak,
            string source,
            NeonTone tone,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            try
            {
                var finalText = await StreamChatAsync(text, speak, cancellationToken).ConfigureAwait(false);

                if (speak && _audioPlayer != null && !string.IsNullOrWhiteSpace(finalText))
                {
                    await SpeakAsync(finalText, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Events.RaiseError($"SendTextAsync failed: {ex.Message}");
            }
        }

        // ====================================================================
        // PUBLIC — VOICE INPUT
        // ====================================================================
        public async Task SendVoiceAsync(
            INeonMicrophoneSource mic,
            bool speak,
            NeonTone tone,
            CancellationToken cancellationToken = default)
        {
            if (mic == null)
                return;

            try
            {
                var wavBytes = await mic.CaptureAsync(cancellationToken).ConfigureAwait(false);
                if (wavBytes == null || wavBytes.Length == 0)
                    return;

                var voiceUrl = new Uri(_baseUri, "/api/chat/voice?speak=true");

                using var content = new ByteArrayContent(wavBytes);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");

                using var resp = await _http.PostAsync(voiceUrl, content, cancellationToken).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("text", out var textProp))
                {
                    Events.RaiseError("Voice response missing 'text' field.");
                    return;
                }

                var reply = textProp.GetString() ?? string.Empty;
                if (reply.Length == 0)
                    return;

                Events.RaiseToken(reply);
                Events.RaiseComplete();

                if (speak && _audioPlayer != null)
                {
                    await SpeakAsync(reply, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Events.RaiseError($"SendVoiceAsync failed: {ex.Message}");
            }
        }

        // ====================================================================
        // INTERNAL — TEXT STREAMING (SSE-like line streaming)
        // ====================================================================
        private async Task<string> StreamChatAsync(
            string message,
            bool speak,
            CancellationToken cancellationToken)
        {
            var finalBuilder = new StringBuilder();

            var urlBuilder = new StringBuilder();
            urlBuilder.Append(_baseUri.AbsoluteUri.TrimEnd('/'));
            urlBuilder.Append("/api/chat/stream?message=");
            urlBuilder.Append(Uri.EscapeDataString(message));
            urlBuilder.Append("&speak=");
            urlBuilder.Append(speak ? "true" : "false");

            var request = new HttpRequestMessage(HttpMethod.Get, urlBuilder.ToString());

            using var resp = await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                    break;

                if (line == "[STREAM_DONE]")
                    break;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                finalBuilder.Append(line);
                Events.RaiseToken(line);
            }

            Events.RaiseComplete();
            return finalBuilder.ToString();
        }

        // ====================================================================
        // INTERNAL — TTS
        // ====================================================================
        private async Task SpeakAsync(string text, CancellationToken cancellationToken)
        {
            if (_audioPlayer == null)
                return;

            var ttsUrl = new Uri(_baseUri, "/api/tts");

            var payload = new
            {
                text = text,
                voice = (string?)null,
                profile = "aethera_prime",
                inline = true
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _http.PostAsync(ttsUrl, content, cancellationToken).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            var wavBytes = await resp.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            await _audioPlayer.PlayAsync(wavBytes, cancellationToken).ConfigureAwait(false);
        }
    }
}
