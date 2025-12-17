using WorkflowManagemetAPI.DTOs;
using WorkflowManagemetAPI.Interfaces;
using WorkflowManagemetAPI.Models;

namespace WorkflowManagemetAPI.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;

        public EmployeeService(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public List<EmployeeTreeDto> GetEmployeeTree()
        {
            var employees = _employeeRepository.GetAll().ToList();

            // ✅ Root'lar ayrı (ManagerId == null)
            var roots = employees.Where(e => e.ManagerId == null).ToList();

            // ✅ Sadece ManagerId null olmayanları dictionary'ye koy
            // key = managerId (null olmayacak)
            var lookup = employees
                .Where(e => e.ManagerId != null)
                .GroupBy(e => e.ManagerId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<EmployeeTreeDto>();

            foreach (var root in roots)
            {
                result.Add(BuildTree(root, lookup));
            }

            return result;
        }

        public Employee CreateEmployee(CreateEmployeeRequest request)
        {
            return ExecuteInTransaction(() =>
            {
                // 1) Manager kontrolü
                Employee? manager = null;
                if (request.ManagerId.HasValue)
                {
                    manager = _employeeRepository.GetByEmployeeId(request.ManagerId.Value);
                    if (manager == null)
                        throw new Exception("Manager bulunamadı.");
                }

                // 2) Employee oluştur (Path yok)
                var employee = new Employee
                {
                    UserId = request.UserId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Phone = request.Phone,
                    SicilNo = request.SicilNo,
                    JobTitle = request.JobTitle,
                    Department = request.Department,
                    ManagerId = request.ManagerId
                };

                // 3) Insert + SaveChanges => EmployeeId oluşur
                _employeeRepository.AddEmployee(employee);
                _employeeRepository.SaveChanges();

                // 4) Path set
                employee.Path = manager == null
                    ? $"/{employee.EmployeeId}/"
                    : $"{manager.Path}{employee.EmployeeId}/";

                // 5) Update + SaveChanges
                _employeeRepository.UpdateEmployee(employee);
                _employeeRepository.SaveChanges();

                return employee;
            });
        }

        private T ExecuteInTransaction<T>(Func<T> action)
        {
            using var tx = _employeeRepository.BeginTransaction();
            try
            {
                var result = action();
                _employeeRepository.Commit(tx);
                return result;
            }
            catch
            {
                _employeeRepository.Rollback(tx);
                throw;
            }
        }

        private EmployeeTreeDto BuildTree(
            Employee employee,
            Dictionary<int, List<Employee>> lookup)
        {
            var dto = new EmployeeTreeDto
            {
                EmployeeId = employee.EmployeeId,
                FullName = employee.FullName,
                JobTitle = employee.JobTitle,
                Department = employee.Department,
                ManagerId = employee.ManagerId,
                Path = employee.Path,
                Children = new List<EmployeeTreeDto>()
            };

            if (lookup.TryGetValue(employee.EmployeeId, out var children))
            {
                foreach (var child in children)
                {
                    dto.Children.Add(BuildTree(child, lookup));
                }
            }

            return dto;
        }
    }
}
