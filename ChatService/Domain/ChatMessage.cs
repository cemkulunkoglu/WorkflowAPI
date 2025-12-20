namespace Workflow.ChatService.Domain
{
    public class ChatMessage
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public string SenderType { get; set; } = "User"; // User | AI
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
