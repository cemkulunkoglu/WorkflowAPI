namespace Workflow.ChatService.Domain
{
    public class ChatThread
    {
        public string ThreadId { get; set; } = Guid.NewGuid().ToString();
        public string ClientId { get; set; } = string.Empty;

        // AI | HUMAN
        public string Type { get; set; } = "AI";

        // Context info (Flow / Task vs.)
        public string? ContextType { get; set; }
        public string? ContextId { get; set; }
        public string? ContextTitle { get; set; }

        public List<ChatMessage> Messages { get; set; } = new();
    }
}
