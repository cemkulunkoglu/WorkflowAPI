using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WorkflowManagemetAPI.DTOs.Employees;
using WorkflowManagemetAPI.Interfaces.Employees;

namespace WorkflowManagemetAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet("tree")]
    [ProducesResponseType(typeof(List<EmployeeTreeDto>), StatusCodes.Status200OK)]
    public ActionResult<List<EmployeeTreeDto>> GetTree()
    {
        var tree = _employeeService.GetEmployeeTree();
        return Ok(tree);
    }

    [HttpGet("detail/{employeeId:int}")]
    [ProducesResponseType(typeof(EmployeeDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<EmployeeDetailDto> GetDetail(int employeeId)
    {
        try
        {
            var emp = _employeeService.GetById(employeeId);

            var dto = new EmployeeDetailDto
            {
                EmployeeId = emp.EmployeeId,
                UserId = emp.UserId,
                FirstName = emp.FirstName,
                LastName = emp.LastName,
                FullName = emp.FullName,
                Phone = emp.Phone,
                SicilNo = emp.SicilNo,
                JobTitle = emp.JobTitle,
                Department = emp.Department,
                ManagerId = emp.ManagerId,
                Path = emp.Path
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("create")]
    public ActionResult<EmployeeResponseDto> Create([FromBody] CreateEmployeeRequest request)
    {
        try
        {
            var created = _employeeService.CreateEmployee(request);

            var dto = new EmployeeResponseDto
            {
                EmployeeId = created.EmployeeId,
                UserId = created.UserId,
                FirstName = created.FirstName,
                LastName = created.LastName,
                FullName = created.FullName,
                Phone = created.Phone,
                SicilNo = created.SicilNo,
                JobTitle = created.JobTitle,
                Department = created.Department,
                ManagerId = created.ManagerId,
                Path = created.Path
            };

            return Created("", dto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("update/{employeeId:int}")]
    [ProducesResponseType(typeof(EmployeeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<EmployeeResponseDto> Update(int employeeId, [FromBody] UpdateEmployeeRequest request)
    {
        try
        {
            request.EmployeeId = employeeId;
            var updated = _employeeService.UpdateEmployee(request);

            var dto = new EmployeeResponseDto
            {
                EmployeeId = updated.EmployeeId,
                UserId = updated.UserId,
                FirstName = updated.FirstName,
                LastName = updated.LastName,
                FullName = updated.FullName,
                Phone = updated.Phone,
                SicilNo = updated.SicilNo,
                JobTitle = updated.JobTitle,
                Department = updated.Department,
                ManagerId = updated.ManagerId,
                Path = updated.Path
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("delete/{employeeId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Delete(int employeeId)
    {
        try
        {
            _employeeService.DeleteEmployee(employeeId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{employeeId:int}/ancestor")]
    [ProducesResponseType(typeof(EmployeeAncestorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<EmployeeAncestorDto> GetAncestor(int employeeId, [FromQuery] int up = 1)
    {
        try
        {
            var dto = _employeeService.GetAncestor(employeeId, up);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{employeeId:int}/ancestors")]
    [ProducesResponseType(typeof(List<EmployeeAncestorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<List<EmployeeAncestorDto>> GetAncestors(
       int employeeId,
       [FromQuery] int depth = 3,
       [FromQuery] bool includeSelf = false)
    {
        try
        {
            var list = _employeeService.GetAncestors(employeeId, depth, includeSelf);
            return Ok(list);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
