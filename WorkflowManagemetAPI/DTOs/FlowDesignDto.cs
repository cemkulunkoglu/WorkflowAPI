using System.Text.Json.Serialization;

namespace WorkflowManagemetAPI.DTOs;

public class FlowDesignDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("designName")]
    public string DesignName { get; init; } = string.Empty;

    [JsonPropertyName("nodes")]
    public required IEnumerable<FlowNodeDto> Nodes { get; init; }

    [JsonPropertyName("edges")]
    public required IEnumerable<FlowEdgeDto> Edges { get; init; }
}

public class FlowNodeDto
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("data")]
    public required FlowNodeDataDto Data { get; init; }

    [JsonPropertyName("position")]
    public required FlowNodePositionDto Position { get; init; }
}

public class FlowNodeDataDto
{
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }
}

public class FlowNodePositionDto
{
    [JsonPropertyName("x")]
    public required double X { get; init; }

    [JsonPropertyName("y")]
    public required double Y { get; init; }
}

public class FlowEdgeDto
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("source")]
    public required string Source { get; init; }

    [JsonPropertyName("target")]
    public required string Target { get; init; }

    [JsonPropertyName("label")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Label { get; init; }

    [JsonPropertyName("style")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FlowEdgeStyleDto? Style { get; init; }

    [JsonPropertyName("labelStyle")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FlowEdgeLabelStyleDto? LabelStyle { get; init; }
}

public class FlowEdgeStyleDto
{
    [JsonPropertyName("stroke")]
    public required string Stroke { get; init; }
}

public class FlowEdgeLabelStyleDto
{
    [JsonPropertyName("fill")]
    public required string Fill { get; init; }
}

