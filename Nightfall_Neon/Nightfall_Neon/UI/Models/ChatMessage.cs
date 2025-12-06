namespace Nightfall.UI.Models
{
    public enum ChatSender
    {
        User,
        Assistant
    }

    public sealed class ChatMessage
    {
        public ChatSender Sender { get; set; }
        public string Text { get; set; } = string.Empty;

        public bool IsUser => Sender == ChatSender.User;
    }
}
