using System.Threading;
using System.Threading.Tasks;

namespace NeonSDK
{
    public interface INeonMicrophoneSource
    {
        /// <summary>
        /// Capture a single utterance as a WAV byte array (16-bit PCM).
        /// </summary>
        Task<byte[]> CaptureAsync(CancellationToken cancellationToken = default);
    }
}
