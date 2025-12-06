using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NeonSDK
{
    public sealed class NeonTtsClient
    {
        private readonly HttpClient _http;

        public NeonTtsClient(HttpClient? httpClient = null)
        {
            _http = httpClient ?? new HttpClient();
        }

        public async Task<byte[]> SynthesizeInlineAsync(
            string text,
            string? voice = null,
            string? profile = null,
            string channel = "system",
            NeonTone? prosody = null,
            CancellationToken cancellationToken = default)
        {
            var url = $"{NeonConfig.VoiceBaseUrl.TrimEnd('/')}/api/tts";

            var body = new
            {
                text,
                voice = voice ?? NeonConfig.DefaultVoice,
                profile = profile ?? NeonConfig.DefaultVoiceProfile,
                channel,
                inline = true,
                prosody = prosody?.HasValue == true
                    ? new { energy = prosody.Energy, label = prosody.Label }
                    : null
            };

            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _http.PostAsync(url, content, cancellationToken);
            resp.EnsureSuccessStatusCode();

            // Inline mode returns raw WAV
            return await resp.Content.ReadAsByteArrayAsync(cancellationToken);
        }
    }
}
