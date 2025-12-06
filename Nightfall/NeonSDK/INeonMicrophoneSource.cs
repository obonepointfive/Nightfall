using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeonSDK
{
    // You can implement this later with NAudio or WASAPI.
    public interface INeonMicrophoneSource
    {
        /// <summary>
        /// Capture a single utterance and return raw WAV bytes.
        /// This call should block until recording is finished (press-to-talk, VAD, etc.).
        /// </summary>
        Task<byte[]> CaptureUtteranceAsync(CancellationToken cancellationToken = default);
    }
}
