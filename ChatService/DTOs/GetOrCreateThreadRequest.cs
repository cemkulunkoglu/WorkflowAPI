namespace Workflow.ChatService.DTOs
{
    public class GetOrCreateThreadRequest
    {
        public string ClientId { get; set; } = string.Empty; // şimdilik UI gönderiyor
        public string Type { get; set; } = "AI"; // "AI" | "HUMAN"

        public string? ContextType { get; set; } // "FlowDesign" | "Task" vs.
        public string? ContextId { get; set; }   // "123"
        public string? ContextTitle { get; set; } // UI'da görünen başlık
    }
}
