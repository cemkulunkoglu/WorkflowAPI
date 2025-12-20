namespace Workflow.ChatService.DTOs
{
    public class SendMessageRequest
    {
        public string Text { get; set; } = string.Empty;
        public string SenderType { get; set; } = "User"; // "User" | "AI"
    }
}
