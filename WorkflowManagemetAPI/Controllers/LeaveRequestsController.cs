using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowManagemetAPI.DTOs.LeaveRequests;
using WorkflowManagemetAPI.Interfaces.LeaveRequests;

namespace WorkflowManagemetAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LeaveRequestsController : ControllerBase
    {
        private readonly ILeaveRequestService _leaveRequestService;

        public LeaveRequestsController(ILeaveRequestService leaveRequestService)
        {
            _leaveRequestService = leaveRequestService;
        }

        [HttpGet("mine")]
        public IActionResult Mine()
        {
            var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "employeeId");
            if (employeeIdClaim == null)
                return Unauthorized("employeeId claim not found.");

            if (!int.TryParse(employeeIdClaim.Value, out var employeeId))
                return Unauthorized("Invalid employeeId claim.");

            var result = _leaveRequestService.GetMine();

            return Ok(result);
        }


        [HttpPost("create")]
        public IActionResult Create([FromBody] CreateLeaveRequestRequest request)
        {
            if (request == null)
                return BadRequest("Geçersiz istek.");

            var result = _leaveRequestService.Create(request);
            return Ok(result);
        }
    }
}
