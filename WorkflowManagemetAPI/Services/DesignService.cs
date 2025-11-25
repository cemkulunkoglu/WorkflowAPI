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

    // YARDIMCI METOT: Token içindeki User ID'yi (sub) okur
    private string GetCurrentUserId()
    {
        // Token'daki "sub" (Subject) claim'i, .NET'te NameIdentifier'a denk gelir.
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            // Eğer [Authorize] attribute'ü varsa buraya düşmesi zordur ama yine de önlem alalım.
            throw new UnauthorizedAccessException("Kullanıcı kimliği (Token) doğrulanamadı.");
        }

        return userId;
    }

    // --- OKUMA (READ) ---
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
                Id = n.Id.ToString(),
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

    public IEnumerable<FlowDesignDto> GetAllFlowDesigns()
    {
        // Sadece giriş yapan kullanıcının tasarımlarını getiriyoruz:
        // var currentUserId = GetCurrentUserId();
        // var designs = _flowDesignRepository.Get(x => x.OwnerUser == currentUserId);

        // Şimdilik hepsini getiriyoruz:
        var result = new List<FlowDesignDto>();
        var designs = _flowDesignRepository.GetAll();

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

            // 👇 3. KRİTİK NOKTA: Tasarımın sahibini Token'dan alıp kaydediyoruz!
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

            // Burada da güvenlik kontrolü yapabilirsin:
            // if (existingDesign.OwnerUser != GetCurrentUserId()) throw ...

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