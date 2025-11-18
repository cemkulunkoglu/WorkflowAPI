using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WorkflowManagemetAPI.DTOs;
using WorkflowManagemetAPI.Interfaces;

namespace WorkflowManagemetAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowEngineController : ControllerBase
{
    private readonly IDesignService _designService;

    public WorkflowEngineController(IDesignService designService)
    {
        _designService = designService;
    }

    [HttpGet("flow-designs")]
    [ProducesResponseType(typeof(IEnumerable<FlowDesignDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<FlowDesignDto>> GetAllDesigns()
    {
        var designs = _designService.GetAllFlowDesigns();
        return Ok(designs);
    }

    [HttpGet("flow-design/{id:int}")]
    [ProducesResponseType(typeof(FlowDesignDto), StatusCodes.Status200OK)]
    public ActionResult<FlowDesignDto> GetDesignById(int id)
    {
        var design = _designService.GetFlowDesignById(id);
        return Ok(design);
    }

    [HttpPost("flow-design/create")]
    [ProducesResponseType(typeof(FlowDesignDto), StatusCodes.Status201Created)]
    public ActionResult<FlowDesignDto> CreateFlowDesign([FromBody] CreateFlowDesignRequest request)
    {
        var createdDesign = _designService.CreateFlowDesign(request);
        return CreatedAtAction(nameof(GetDesignById), new { id = createdDesign.Nodes.First().Id }, createdDesign);
    }

}
