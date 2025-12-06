using System;

namespace NeonSDK
{
    public sealed class NeonEvents
    {
        public event Action<string>? AssistantToken;
        public event Action? AssistantComplete;
        public event Action<string>? Error;
        public event Action<byte[]>? VoiceReady;

        internal void RaiseToken(string text) =>
            AssistantToken?.Invoke(text);

        internal void RaiseComplete() =>
            AssistantComplete?.Invoke();

        internal void RaiseError(string text) =>
            Error?.Invoke(text);

        internal void RaiseVoiceReady(byte[] wav) =>
            VoiceReady?.Invoke(wav);
    }
}
