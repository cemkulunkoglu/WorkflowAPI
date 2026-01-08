using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WorkflowManagemetAPI.DTOs;
using WorkflowManagemetAPI.Extensions;
using WorkflowManagemetAPI.Interfaces;

namespace WorkflowManagemetAPI.Services;

public class DesignService : IDesignService
{
    private readonly IFlowDesignRepository _flowDesignRepository;
    private readonly IFlowNodeRepository _flowNodeRepository;

    // Token'a erişmemizi sağlayan servis
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DesignService(
        IFlowDesignRepository flowDesignRepository,
        IFlowNodeRepository flowNodeRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _flowDesignRepository = flowDesignRepository;
        _flowNodeRepository = flowNodeRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    private string GetCurrentUserId()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("Kullanıcı kimliği (Token) doğrulanamadı.");
        }

        return userId;
    }


    public FlowDesignDto GetFlowDesignById(int designId)
    {
        var design = _flowDesignRepository.GetByID(designId)
            ?? throw new Exception($"Design bulunamadı (ID={designId}).");

        // Burada istersen güvenlik kontrolü yapabilirsin:
        // if (design.OwnerUser != GetCurrentUserId()) throw new Exception("Bu tasarımı görmeye yetkiniz yok.");

        var allNodes = _flowNodeRepository.GetByDesignId(designId).ToList();

        var nodeDtos = allNodes
            .Where(n => !string.Equals(n.NodeType, "transition", StringComparison.OrdinalIgnoreCase))
            .Select(n => new FlowNodeDto
            {
                Id = !string.IsNullOrWhiteSpace(n.ClientId) ? n.ClientId : n.Id.ToString(),
                Type = n.Type,
                Data = new FlowNodeDataDto { Label = n.Label, Type = n.Type },
                Position = new FlowNodePositionDto { X = n.Posx, Y = n.Posy }
            }).ToList();

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
            }).ToList();

        return new FlowDesignDto
        {
            Id = designId,
            DesignName = design.DesignName,
            Nodes = nodeDtos,
            Edges = edgeDtos
        };
    }


    public IEnumerable<FlowDesignDto> GetFlowDesignsByUserId()
    {
        var currentUserId = GetCurrentUserId();

        var result = new List<FlowDesignDto>();
        var designs = _flowDesignRepository.GetDesignByUserId(currentUserId);

        foreach (var design in designs)
        {
            result.Add(GetFlowDesignById(design.Id));
        }
        return result;
    }


    public IEnumerable<FlowDesignDto> GetAllFlowDesigns()
    {
        var designs = _flowDesignRepository.GetAll();
        var result = new List<FlowDesignDto>();

        foreach (var design in designs)
        {
            result.Add(GetFlowDesignById(design.Id));
        }
        return result;
    }


    public FlowDesignDto CreateFlowDesign(CreateFlowDesignRequest request)
    {
        return ExecuteInTransaction(() =>
        {
            var design = request.ToEntity();
            design.OwnerUser = GetCurrentUserId();

            _flowDesignRepository.Add(design);

            var nodes = request.Nodes.ToNodeEntities(design.Id);
            var edges = request.Edges.ToEdgeEntities(design.Id);

            _flowNodeRepository.AddRange(nodes);
            _flowNodeRepository.AddRange(edges);

            return GetFlowDesignById(design.Id);
        });
    }


    public FlowDesignDto UpdateFlowDesign(int id, CreateFlowDesignRequest request)
    {
        return ExecuteInTransaction(() =>
        {
            var existingDesign = _flowDesignRepository.GetByID(id);
            if (existingDesign == null)
                throw new Exception($"Güncellenecek tasarım bulunamadı (ID={id})");

            existingDesign.DesignName = request.DesignName;
            existingDesign.UpdatedDate = DateTime.Now;

            _flowDesignRepository.UpdateDesign(existingDesign);
            _flowNodeRepository.DeleteByDesignId(id);

            var newNodes = request.Nodes.ToNodeEntities(id);
            var newEdges = request.Edges.ToEdgeEntities(id);

            _flowNodeRepository.AddRange(newNodes);
            _flowNodeRepository.AddRange(newEdges);

            return GetFlowDesignById(id);
        });
    }


    public void DeleteFlowDesign(int designId)
    {
        ExecuteInTransaction(() =>
        {
            var design = _flowDesignRepository.GetByID(designId);
            if (design == null)
                throw new Exception($"Tasarım bulunamadı (ID={designId})");

            if (design.OwnerUser != GetCurrentUserId())
                throw new UnauthorizedAccessException("Yetkiniz yok.");

            _flowNodeRepository.DeleteByDesignId(designId);
            _flowDesignRepository.DeleteDesign(design);

            return true;
        });
    }


    private T ExecuteInTransaction<T>(Func<T> action)
    {
        using var transaction = _flowDesignRepository.BeginTransaction();
        try
        {
            var result = action();
            _flowDesignRepository.Commit(transaction);
            return result;
        }
        catch
        {
            _flowDesignRepository.Rollback(transaction);
            throw;
        }
    }

}