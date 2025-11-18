using WorkflowManagemetAPI.DTOs;
using WorkflowManagemetAPI.Interfaces;
using WorkflowManagemetAPI.Models;
using WorkflowManagemetAPI.Repositories;

namespace WorkflowManagemetAPI.Services;

public class DesignService : IDesignService
{
    private readonly IFlowDesignRepository flowDesignRepository;
    private readonly IFlowNodeRepository flowNodeRepository;

    public DesignService(IFlowDesignRepository flowDesignRepository, IFlowNodeRepository flowNodeRepository)
    {
        this.flowDesignRepository = flowDesignRepository;
		this.flowNodeRepository = flowNodeRepository;
	}

    public FlowDesignDto GetFlowDesignById(int designId)
    {
        var design = flowDesignRepository.GetByID(designId)
            ?? throw new Exception($"Design bulunamadı (ID={designId}).");

        var allNodes = flowNodeRepository.GetByDesignId(designId).ToList();

        var nodeDtos = allNodes
            .Where(n => !string.Equals(n.NodeType, "transition", StringComparison.OrdinalIgnoreCase))
            .Select(n => new FlowNodeDto
            {
                Id = n.Id.ToString(),
                Type = n.Type,
                Data = new FlowNodeDataDto { Label = n.Label, Type = n.Type },
                Position = new FlowNodePositionDto { X = n.Posx, Y = n.Posy }
            })
            .ToList();

        var edgeDtos = allNodes
            .Where(n => string.Equals(n.NodeType, "transition", StringComparison.OrdinalIgnoreCase))
            .Select(n => new FlowEdgeDto
            {
                Id = $"e{n.Id}",
                Source = n.Source ?? "",
                Target = n.Target ?? "",
                Label = n.Label,
                Style = n.Style is null ? null : new FlowEdgeStyleDto { Stroke = n.Style.Stroke ?? "#000000" },
                LabelStyle = n.LabelStyle is null ? null : new FlowEdgeLabelStyleDto { Fill = n.LabelStyle.Fill ?? "#000000" }
            })
            .ToList();

        return new FlowDesignDto { Nodes = nodeDtos, Edges = edgeDtos };
    }

    public IEnumerable<FlowDesignDto> GetAllFlowDesigns()
    {
        var result = new List<FlowDesignDto>();
        var designs = flowDesignRepository.GetAll();

        foreach (var design in designs)
        {
            result.Add(GetFlowDesignById(design.Id));
        }

        return result;
    }

    public FlowDesignDto CreateFlowDesign(CreateFlowDesignRequest request)
    {
        var design = new FlowDesign
        {
            DesignName = request.DesignName,
            OwnerUser = null,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now,
            IsActive = true,
            Description = null
        };

        flowDesignRepository.Insert(design);
        flowDesignRepository.SaveChanges(); 

        var nodeEntities = request.Nodes.Select((n, index) => new FlowNode
        {
            DesignId = design.Id,
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

        flowNodeRepository.InsertRange(nodeEntities);
        flowNodeRepository.SaveChanges();

        var edgeEntities = request.Edges.Select(e => new FlowNode
        {
            DesignId = design.Id,
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

        flowNodeRepository.InsertRange(edgeEntities);
        flowNodeRepository.SaveChanges();

        return GetFlowDesignById(design.Id);
    }
}
