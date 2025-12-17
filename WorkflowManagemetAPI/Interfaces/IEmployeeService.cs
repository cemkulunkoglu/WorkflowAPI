using WorkflowManagemetAPI.DTOs;
using WorkflowManagemetAPI.Models;

namespace WorkflowManagemetAPI.Interfaces
{
    public interface IEmployeeService
    {
        Employee CreateEmployee(CreateEmployeeRequest request);
        List<EmployeeTreeDto> GetEmployeeTree();
    }
}
