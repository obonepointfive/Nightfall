using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace NeonSDK
{
    /// <summary>
    /// A simple, clean, async WAV player using NAudio.
    /// Supports cancellation and ensures proper resource cleanup.
    /// </summary>
    public sealed class NeonAudioPlayer : INeonAudioPlayer, IDisposable
    {
        private readonly object _lock = new();
        private WaveOutEvent? _outputDevice;
        private WaveStream? _waveStream;

        public async Task PlayAsync(byte[] wavBytes, CancellationToken cancellationToken = default)
        {
            // NAudio is not thread-safe — wrap everything
            lock (_lock)
            {
                StopInternal();

                _waveStream = new WaveFileReader(new MemoryStream(wavBytes));
                _outputDevice = new WaveOutEvent
                {
                    DesiredLatency = 80 // ms, low latency
                };

                _outputDevice.Init(_waveStream);
                _outputDevice.Play();
            }

            // Wait until playback finishes or is canceled
            while (_outputDevice?.PlaybackState == PlaybackState.Playing)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    StopInternal();
                    return;
                }

                await Task.Delay(10, cancellationToken);
            }

            StopInternal();
        }

        private void StopInternal()
        {
            try
            {
                _outputDevice?.Stop();
                _waveStream?.Dispose();
                _outputDevice?.Dispose();
            }
            catch { /* ignore */ }
            finally
            {
                _waveStream = null;
                _outputDevice = null;
            }
        }

        public void Dispose() => StopInternal();
    }
}
