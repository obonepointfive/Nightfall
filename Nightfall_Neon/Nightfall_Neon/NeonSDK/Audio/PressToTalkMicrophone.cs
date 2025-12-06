using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace NeonSDK
{
    /// <summary>
    /// Push-to-talk microphone using NAudio.
    /// Captures PCM in memory, exports WAV as byte[].
    /// </summary>
    public sealed class PressToTalkMicrophone : INeonMicrophoneSource, IDisposable
    {
        private WaveInEvent? _waveIn;
        private MemoryStream? _audioStream;
        private WaveFileWriter? _writer;
        private readonly object _lock = new();
        private bool _isRecording;

        public bool IsRecording => _isRecording;

        public int SampleRate { get; }
        public int Channels { get; }

        public PressToTalkMicrophone(int sampleRate = 44100, int channels = 1)
        {
            SampleRate = sampleRate;
            Channels = channels;
        }

        // ----------------------------------------------------------
        // START RECORDING
        // ----------------------------------------------------------
        public void StartRecording()
        {
            lock (_lock)
            {
                if (_isRecording)
                    return;

                _audioStream = new MemoryStream();
                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(SampleRate, 16, Channels),
                    BufferMilliseconds = 50
                };

                _writer = new WaveFileWriter(new IgnoreDisposeStream(_audioStream), _waveIn.WaveFormat);
                _waveIn.DataAvailable += OnDataAvailable;

                _waveIn.StartRecording();
                _isRecording = true;
            }
        }

        // ----------------------------------------------------------
        // STOP RECORDING
        // ----------------------------------------------------------
        public void StopRecording()
        {
            lock (_lock)
            {
                if (!_isRecording)
                    return;

                _isRecording = false;

                try
                {
                    _waveIn?.StopRecording();
                    _writer?.Flush();
                }
                catch { }

                _waveIn?.Dispose();
                _writer?.Dispose();

                _waveIn = null;
                _writer = null;
            }
        }

        // ----------------------------------------------------------
        // AUDIO EVENT
        // ----------------------------------------------------------
        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            lock (_lock)
            {
                if (_isRecording && _writer != null)
                {
                    _writer.Write(e.Buffer, 0, e.BytesRecorded);
                }
            }
        }

        // ----------------------------------------------------------
        // RETURN WAV AS BYTE[]
        // ----------------------------------------------------------
        public async Task<byte[]> CaptureAsync(CancellationToken cancellationToken = default)
        {
            StopRecording();

            if (_audioStream == null)
                return Array.Empty<byte>();

            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    var finalWav = new MemoryStream();

                    using (var writer = new WaveFileWriter(finalWav, new WaveFormat(SampleRate, 16, Channels)))
                    {
                        _audioStream.Position = 0;
                        _audioStream.CopyTo(writer);
                    }

                    return finalWav.ToArray();
                }

            }, cancellationToken);
        }

        // ----------------------------------------------------------
        // CLEANUP
        // ----------------------------------------------------------
        public void Dispose()
        {
            lock (_lock)
            {
                try
                {
                    _waveIn?.Dispose();
                    _writer?.Dispose();
                    _audioStream?.Dispose();
                }
                catch { }
            }
        }

        // ----------------------------------------------------------
        // Prevent WaveFileWriter from prematurely disposing underlying stream
        // ----------------------------------------------------------
        private sealed class IgnoreDisposeStream : Stream
        {
            private readonly Stream _inner;

            public IgnoreDisposeStream(Stream inner) => _inner = inner;

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => _inner.CanWrite;
            public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => _inner.Position = value; }

            public override void Flush() => _inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
            public override void SetLength(long value) => _inner.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

            protected override void Dispose(bool disposing) { /* suppress base dispose */ }
        }
    }
}
