using System.Text.Json.Serialization;

namespace WorkflowManagemetAPI.DTOs
{
    public class CreateFlowEdgeRequest
    {
        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("target")]
        public string Target { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string? Label { get; set; }
    }
}
