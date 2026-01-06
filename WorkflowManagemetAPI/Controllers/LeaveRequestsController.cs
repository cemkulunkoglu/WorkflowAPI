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
