using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WorkflowManagemetAPI.DTOs
{
    public class CreateFlowDesignRequest
    {
        [JsonPropertyName("designName")]
        public string DesignName { get; set; } = string.Empty;

        [JsonPropertyName("nodes")]
        public List<CreateFlowNodeRequest> Nodes { get; set; } = new();

        [JsonPropertyName("edges")]
        public List<CreateFlowEdgeRequest> Edges { get; set; } = new();
    }

    public class CreateFlowNodeRequest
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }
    }

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
