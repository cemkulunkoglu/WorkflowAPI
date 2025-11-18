using Newtonsoft.Json;

namespace WorkflowManagemetAPI.Models
{
    public class FlowNode
    {
        public int Id { get; set; }
        public int DesignId { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string NodeType { get; set; } = string.Empty;
        public string? Source { get; set; }
        public string? Target { get; set; }
        public LabelStyle? LabelStyle { get; set; }
        public Style? Style { get; set; }

        public double Posx { get; set; }
        public double Posy { get; set; }
        public int Order { get; set; }
    }

    public class LabelStyle
    {
        public string? Fill { get; set; }
        public string? Stroke { get; set; }
    }

    public class Style
    {
        public string? Stroke { get; set; }
    }
}
