using WorkflowManagemetAPI.Models;

namespace WorkflowManagemetAPI.Interfaces
{
    public interface IEmployeeRepository : IWorkflowUnitOfWork<Employee>
    {
        IEnumerable<Employee> GetAll();
        Employee? GetByEmployeeId(int employeeId);
        IEnumerable<Employee> GetByManagerId(int? managerId);
        List<Employee> GetByEmployeeIds(IEnumerable<int> employeeIds);

        void AddEmployee(Employee employee);
        void UpdateEmployee(Employee employee);
        void DeleteEmployee(Employee employee);
    }
}
