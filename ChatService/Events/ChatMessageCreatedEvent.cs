namespace Workflow.ChatService.Events
{
    public class ChatMessageCreatedEvent
    {
        public string ThreadId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;

        public string MessageId { get; set; } = string.Empty;
        public string SenderType { get; set; } = "User"; // User | AI
        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public string? ReplyToMessageId { get; set; }

    }
}
