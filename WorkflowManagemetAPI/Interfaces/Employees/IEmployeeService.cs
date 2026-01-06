using WorkflowManagemetAPI.DTOs.Employees;
using WorkflowManagemetAPI.Models.Employees;

namespace WorkflowManagemetAPI.Interfaces.Employees
{
    public interface IEmployeeService
    {
        List<EmployeeTreeDto> GetEmployeeTree();

        Employee GetById(int employeeId);

        Employee CreateEmployee(CreateEmployeeRequest request);

        Employee UpdateEmployee(UpdateEmployeeRequest request);

        void DeleteEmployee(int employeeId);
        EmployeeAncestorDto GetAncestor(int employeeId, int up);
        List<EmployeeAncestorDto> GetAncestors(int employeeId, int depth, bool includeSelf = false);

    }
}
