using WorkflowManagemetAPI.DTOs;
using WorkflowManagemetAPI.Models;

namespace WorkflowManagemetAPI.Extensions;

public static class FlowMappingExtensions
{
    public static FlowDesign ToEntity(this CreateFlowDesignRequest request)
    {
        return new FlowDesign
        {
            DesignName = request.DesignName,
            OwnerUser = null,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now,
            IsActive = true,
            Description = null
        };
    }

    public static List<FlowNode> ToNodeEntities(this List<CreateFlowNodeRequest> nodes, int designId)
    {
        return nodes.Select((n, index) => new FlowNode
        {
            DesignId = designId,
            Label = n.Label,
            Type = n.Type,
            NodeType = "node",
            Source = null,
            Target = null,
            Posx = n.X,
            Posy = n.Y,
            Order = index,
            LabelStyle = null,
            Style = null
        }).ToList();
    }
    public static List<FlowNode> ToEdgeEntities(this List<CreateFlowEdgeRequest> edges, int designId)
    {
        return edges.Select(e => new FlowNode
        {
            DesignId = designId,
            Label = e.Label,
            Type = "",
            NodeType = "transition",
            Source = e.Source,
            Target = e.Target,
            Posx = 0,
            Posy = 0,
            Order = 0,
            LabelStyle = null,
            Style = null
        }).ToList();
    }



}