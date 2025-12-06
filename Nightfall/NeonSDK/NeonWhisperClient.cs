using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace NeonSDK
{
    public sealed class NeonWhisperClient
    {
        private readonly HttpClient _http;

        public NeonWhisperClient(HttpClient? httpClient = null)
        {
            _http = httpClient ?? new HttpClient();
        }

        public async Task<(string text, float confidence)> TranscribeAsync(
            byte[] wavData,
            CancellationToken cancellationToken = default)
        {
            var url = $"{NeonConfig.WhisperBaseUrl.TrimEnd('/')}/api/whisper/transcribe";

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(wavData);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
            content.Add(fileContent, "file", "audio.wav");

            using var resp = await _http.PostAsync(url, content, cancellationToken);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            var root = doc.RootElement;
            var text = root.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
            var conf = root.TryGetProperty("confidence", out var c) ? c.GetSingle() : 0f;

            return (text, conf);
        }
    }
}
