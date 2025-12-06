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
        private readonly NeonWhisperClient _whisper;
        private readonly NeonTtsClient _tts;
        private readonly INeonAudioPlayer? _audioPlayer;

        public NeonEvents Events { get; } = new();

        public NeonClient(
            INeonAudioPlayer? audioPlayer = null,
            HttpClient? httpClient = null)
        {
            _http = httpClient ?? new HttpClient();
            _whisper = new NeonWhisperClient(_http);
            _tts = new NeonTtsClient(_http);
            _audioPlayer = audioPlayer;
        }

        // -------------------------------
        // Typed message → stream reply
        // -------------------------------
        public async Task SendTextAsync(
            string message,
            bool speak = false,
            string source = "typed",
            NeonTone? tone = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            var gateway = NeonConfig.GatewayBaseUrl.TrimEnd('/');
            var urlBuilder = new StringBuilder($"{gateway}/api/chat/stream?message=");
            urlBuilder.Append(Uri.EscapeDataString(message));
            urlBuilder.Append("&speak=").Append(speak ? "true" : "false");
            urlBuilder.Append("&source=").Append(Uri.EscapeDataString(source));

            if (tone?.HasValue == true)
            {
                if (!string.IsNullOrWhiteSpace(tone.Energy))
                    urlBuilder.Append("&tone_energy=").Append(Uri.EscapeDataString(tone.Energy));
                if (!string.IsNullOrWhiteSpace(tone.Label))
                    urlBuilder.Append("&tone_label=").Append(Uri.EscapeDataString(tone.Label));
            }

            var url = urlBuilder.ToString();

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var resp = await _http.SendAsync(
                req,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var sb = new StringBuilder();

            try
            {
                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    JsonElement root;
                    try
                    {
                        root = JsonSerializer.Deserialize<JsonElement>(line);
                    }
                    catch
                    {
                        // Raw/unparseable line—ignore
                        continue;
                    }

                    var type = root.TryGetProperty("type", out var t)
                        ? t.GetString()
                        : null;

                    switch (type)
                    {
                        case "assistant_token":
                            var text = root.TryGetProperty("text", out var txt)
                                ? txt.GetString() ?? ""
                                : "";
                            if (!string.IsNullOrEmpty(text))
                            {
                                sb.Append(text);
                                Events.RaiseToken(text);
                            }
                            break;

                        case "error":
                            var errText = root.TryGetProperty("text", out var et)
                                ? et.GetString() ?? "Unknown error"
                                : "Unknown error";
                            Events.RaiseError(errText);
                            return;

                        case "complete":
                            Events.RaiseComplete();
                            if (speak && _audioPlayer != null)
                            {
                                var fullText = sb.ToString();
                                if (!string.IsNullOrWhiteSpace(fullText))
                                {
                                    var wav = await _tts.SynthesizeInlineAsync(
                                        fullText,
                                        prosody: tone,
                                        cancellationToken: cancellationToken);

                                    Events.RaiseVoiceReady(wav);
                                    await _audioPlayer.PlayAsync(wav);
                                }
                            }
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                Events.RaiseError($"Stream error: {ex.Message}");
            }
        }

        // ----------------------------------------
        // Voice message (mic) → Whisper → Gateway
        // ----------------------------------------
        public async Task SendVoiceAsync(
            INeonMicrophoneSource mic,
            bool speak = true,
            NeonTone? tone = null,
            CancellationToken cancellationToken = default)
        {
            // 1) Capture WAV from mic
            var wav = await mic.CaptureUtteranceAsync(cancellationToken);
            if (wav == null || wav.Length == 0)
            {
                Events.RaiseError("No audio captured from microphone.");
                return;
            }

            // 2) Transcribe
            var (text, confidence) = await _whisper.TranscribeAsync(wav, cancellationToken);
            if (string.IsNullOrWhiteSpace(text))
            {
                Events.RaiseError("Whisper returned empty transcript.");
                return;
            }

            // You can decide if you want to gate on confidence.
            // For now, always send.
            await SendTextAsync(
                text,
                speak: speak,
                source: "mic",
                tone: tone,
                cancellationToken: cancellationToken);
        }

        public void Dispose()
        {
            _http.Dispose();
        }
    }
}
