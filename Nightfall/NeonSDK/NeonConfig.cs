using System;

namespace NeonSDK
{
    public static class NeonConfig
    {
        // Default URLs assuming Docker ports:
        // gateway:        8000
        // whisper-service:8005
        // voice-service:  8004

        public static string GatewayBaseUrl { get; set; } =
            Environment.GetEnvironmentVariable("AETHERA_GATEWAY_URL")
            ?? "http://localhost:8000";

        public static string WhisperBaseUrl { get; set; } =
            Environment.GetEnvironmentVariable("AETHERA_WHISPER_URL")
            ?? "http://localhost:8005";

        public static string VoiceBaseUrl { get; set; } =
            Environment.GetEnvironmentVariable("AETHERA_VOICE_URL")
            ?? "http://localhost:8004";

        public static string DefaultVoice { get; set; } =
            Environment.GetEnvironmentVariable("AETHERA_VOICE_MODEL")
            ?? "en_US-libritts_r-medium";

        public static string DefaultVoiceProfile { get; set; } =
            Environment.GetEnvironmentVariable("AETHERA_VOICE_PROFILE")
            ?? "aethera_prime";
    }
}
