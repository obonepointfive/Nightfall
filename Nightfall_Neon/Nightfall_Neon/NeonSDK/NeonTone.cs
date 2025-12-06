namespace NeonSDK
{
    public sealed class NeonTone
    {
        public string? Energy { get; set; }  // e.g. "low" | "medium" | "high" | "0.82"
        public string? Label { get; set; }   // e.g. "calm", "focused", "determined"

        public bool HasValue =>
            !string.IsNullOrWhiteSpace(Energy) ||
            !string.IsNullOrWhiteSpace(Label);
    }
}
