using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowManagemetAPI.DTOs;
using WorkflowManagemetAPI.Interfaces;

namespace WorkflowManagemetAPI.Controllers
{
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
        public IActionResult GetTree()
        {
            var tree = _employeeService.GetEmployeeTree();
            return Ok(tree);
        }

        [HttpPost]
        public IActionResult Create([FromBody] CreateEmployeeRequest request)
        {
            var created = _employeeService.CreateEmployee(request);
            return Ok(created);
        }
    }
}
