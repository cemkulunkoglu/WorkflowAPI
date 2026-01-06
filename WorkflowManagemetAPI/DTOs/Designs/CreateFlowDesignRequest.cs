using System.Text.Json.Serialization;

namespace WorkflowManagemetAPI.DTOs.Designs
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
}
