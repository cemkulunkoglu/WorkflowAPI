namespace Workflow.ChatService.DTOs
{
    public class SendMessageRequest
    {
        public string? ThreadId { get; set; }   // şimdilik global
        public string Text { get; set; } = string.Empty;
        public string SenderType { get; set; } = "User"; // "User" | "AI"
    }
}
