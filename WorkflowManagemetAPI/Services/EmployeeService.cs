using WorkflowManagemetAPI.DTOs;
using WorkflowManagemetAPI.Interfaces;
using WorkflowManagemetAPI.Models;
using WorkflowManagemetAPI.Repositories;
using WorkflowManagemetAPI.UnitOfWork;

namespace WorkflowManagemetAPI.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeUnitOfWork _uow;

        public EmployeeService(IEmployeeUnitOfWork uow)
        {
            _uow = uow;
        }

        public List<EmployeeTreeDto> GetEmployeeTree()
        {
            var employees = _uow.Employees.GetAll().ToList();

            var roots = employees.Where(e => e.ManagerId == null).ToList();

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

        public Employee GetById(int employeeId)
        {
            return _uow.Employees.GetByEmployeeId(employeeId)
                   ?? throw new Exception("Employee bulunamadı.");
        }

        public Employee CreateEmployee(CreateEmployeeRequest request)
        {
            return ExecuteInTransaction(() =>
            {
                // 1) Manager kontrol
                Employee? manager = null;
                if (request.ManagerId.HasValue)
                {
                    manager = _uow.Employees.GetByEmployeeId(request.ManagerId.Value);
                    if (manager == null)
                        throw new Exception("Manager bulunamadı.");
                }

                // 2) Employee oluştur (Path daha sonra)
                var employee = new Employee
                {
                    UserId = request.UserId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Phone = request.Phone,
                    SicilNo = request.SicilNo,
                    JobTitle = request.JobTitle,
                    Department = request.Department,
                    ManagerId = request.ManagerId,
                    Path = null
                };

                // 3) Insert => EmployeeId oluşsun
                _uow.Employees.AddEmployee(employee);
                _uow.SaveChanges();

                // 4) Path set et
                employee.Path = manager == null
                    ? $"/{employee.EmployeeId}/"
                    : $"{(string.IsNullOrWhiteSpace(manager.Path) ? $"/{manager.EmployeeId}/" : manager.Path)}{employee.EmployeeId}/";

                // 5) Update
                _uow.Employees.UpdateEmployee(employee);
                _uow.SaveChanges();

                return employee;
            });
        }

        public Employee UpdateEmployee(UpdateEmployeeRequest request)
        {
            return ExecuteInTransaction(() =>
            {
                var emp = _uow.Employees.GetByEmployeeId(request.EmployeeId)
                          ?? throw new Exception("Employee bulunamadı");

                var oldManagerId = emp.ManagerId;

                // alanları güncelle
                emp.FirstName = request.FirstName;
                emp.LastName = request.LastName;
                emp.Phone = request.Phone;
                emp.SicilNo = request.SicilNo;
                emp.JobTitle = request.JobTitle;
                emp.Department = request.Department;
                emp.UserId = request.UserId;
                emp.ManagerId = request.ManagerId;

                // Manager değiştiyse:
                if (oldManagerId != request.ManagerId)
                {
                    // 1) cycle check
                    EnsureNoCycle(emp.EmployeeId, request.ManagerId);

                    // 2) yeni path hesapla + kaydet
                    emp.Path = BuildNewPath(emp.EmployeeId, request.ManagerId);
                    _uow.Employees.UpdateEmployee(emp);
                    _uow.SaveChanges();

                    // 3) alt ağacı recursive rebuild
                    RebuildChildrenPaths(emp.EmployeeId, emp.Path!);
                }
                else
                {
                    _uow.Employees.UpdateEmployee(emp);
                    _uow.SaveChanges();
                }

                return emp;
            });
        }

        public void DeleteEmployee(int employeeId)
        {
            ExecuteInTransaction(() =>
            {
                var root = _uow.Employees.GetByEmployeeId(employeeId)
                           ?? throw new Exception("Employee bulunamadı");

                DeleteSubtree(root.EmployeeId);
                _uow.SaveChanges();

                return true;
            });
        }

        private EmployeeTreeDto BuildTree(Employee employee, Dictionary<int, List<Employee>> lookup)
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

        private string BuildNewPath(int employeeId, int? managerId)
        {
            if (managerId == null)
                return $"/{employeeId}/";

            var manager = _uow.Employees.GetByEmployeeId(managerId.Value)
                          ?? throw new Exception("Manager bulunamadı");

            var managerPath = string.IsNullOrWhiteSpace(manager.Path)
                ? $"/{manager.EmployeeId}/"
                : manager.Path;

            return $"{managerPath}{employeeId}/";
        }

        private void RebuildChildrenPaths(int parentId, string parentPath)
        {
            var children = _uow.Employees.GetByManagerId(parentId).ToList();

            foreach (var child in children)
            {
                child.Path = $"{parentPath}{child.EmployeeId}/";
                _uow.Employees.UpdateEmployee(child);

                RebuildChildrenPaths(child.EmployeeId, child.Path!);
            }

            _uow.SaveChanges();
        }

        private void DeleteSubtree(int employeeId)
        {
            var children = _uow.Employees.GetByManagerId(employeeId).ToList();

            foreach (var child in children)
                DeleteSubtree(child.EmployeeId);

            var emp = _uow.Employees.GetByEmployeeId(employeeId);
            if (emp != null)
                _uow.Employees.DeleteEmployee(emp);
        }

        private void EnsureNoCycle(int employeeId, int? newManagerId)
        {
            if (newManagerId == null) return;

            if (newManagerId.Value == employeeId)
                throw new Exception("Kişi kendine manager olamaz.");

            var newManager = _uow.Employees.GetByEmployeeId(newManagerId.Value)
                            ?? throw new Exception("Manager bulunamadı");

            if (!string.IsNullOrWhiteSpace(newManager.Path) &&
                newManager.Path.Contains($"/{employeeId}/"))
            {
                throw new Exception("Altındaki birine manager atanamaz (cycle).");
            }
        }

        private T ExecuteInTransaction<T>(Func<T> action)
        {
            using var tx = _uow.BeginTransaction();
            try
            {
                var result = action();
                _uow.Commit(tx);
                return result;
            }
            catch
            {
                _uow.Rollback(tx);
                throw;
            }
        }
    }
}
