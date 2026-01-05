using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

    //[HttpGet("debug/me")]
    //public IActionResult DebugMe()
    //{
    //    var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
    //    return Ok(claims);
    //}

    [HttpGet("flow-designs/mine")]
    [ProducesResponseType(typeof(IEnumerable<FlowDesignDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<FlowDesignDto>> GetMyDesigns()
    {
        var designs = _designService.GetFlowDesignsByUserId();
        return Ok(designs);
    }


    [HttpGet("flow-design/{id:int}")]
    [ProducesResponseType(typeof(FlowDesignDto), StatusCodes.Status200OK)]
    public ActionResult<FlowDesignDto> GetDesignById(int id)
    {
        try
        {
            var design = _designService.GetFlowDesignById(id);
            return Ok(design);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpPost("flow-design/create")]
    [ProducesResponseType(typeof(FlowDesignDto), StatusCodes.Status201Created)]
    public ActionResult<FlowDesignDto> CreateFlowDesign([FromBody] CreateFlowDesignRequest request)
    {
        try
        {
            var createdDesign = _designService.CreateFlowDesign(request);
            return CreatedAtAction(nameof(GetDesignById), new { id = createdDesign.Id }, createdDesign);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpPut("flow-design/update/{id:int}")]
    [ProducesResponseType(typeof(FlowDesignDto), StatusCodes.Status200OK)]
    public ActionResult<FlowDesignDto> UpdateDesign(int id, [FromBody] CreateFlowDesignRequest request)
    {
        try
        {
            var updatedDesign = _designService.UpdateFlowDesign(id, request);
            return Ok(updatedDesign);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpDelete("flow-design/delete/{id:int}")]
    public IActionResult DeleteDesign(int id)
    {
        try
        {
            _designService.DeleteFlowDesign(id);
            return Ok(new { message = $"Tasarým (ID={id}) ve tüm bileþenleri baþarýyla silindi." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
