using WorkflowManagemetAPI.DTOs;
using WorkflowManagemetAPI.Models;

namespace WorkflowManagemetAPI.Interfaces
{
    public interface IEmployeeService
    {
        List<EmployeeTreeDto> GetEmployeeTree();

        Employee GetById(int employeeId);

        Employee CreateEmployee(CreateEmployeeRequest request);

        Employee UpdateEmployee(UpdateEmployeeRequest request);

        void DeleteEmployee(int employeeId);

    }
}
