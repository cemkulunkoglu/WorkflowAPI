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

        public EmployeeAncestorDto GetAncestor(int employeeId, int up)
        {
            if (up < 0) throw new Exception("up negatif olamaz.");

            var emp = GetById(employeeId);
            var pathIds = ParsePathIds(emp.Path);

            // includeSelf=false mantığı: up=0 => kendisi
            var targetIndex = (pathIds.Count - 1) - up;

            if (targetIndex < 0 || targetIndex >= pathIds.Count)
                throw new Exception($"İstenen üst bulunamadı. (up={up})");

            var targetId = pathIds[targetIndex];
            var target = _uow.Employees.GetByEmployeeId(targetId)
                         ?? throw new Exception("Üst employee bulunamadı.");

            return ToAncestorDto(target, pathIds.IndexOf(targetId) + 1);
        }

        public List<EmployeeAncestorDto> GetAncestors(int employeeId, int depth, bool includeSelf = false)
        {
            if (depth <= 0) throw new Exception("depth 1 veya daha büyük olmalı.");

            var emp = GetById(employeeId);
            var pathIds = ParsePathIds(emp.Path);

            // kendisi hariç isteniyorsa, en sondaki (self) düşür
            var idsForChain = includeSelf ? pathIds : pathIds.Take(pathIds.Count - 1).ToList();

            if (idsForChain.Count == 0)
                return new List<EmployeeAncestorDto>();

            // yukarı doğru depth kadar al (sondan)
            var takeCount = Math.Min(depth, idsForChain.Count);
            var slice = idsForChain.Skip(idsForChain.Count - takeCount).ToList();

            // db’den toplu çek
            var employees = _uow.Employees.GetByEmployeeIds(slice);
            var dict = employees.ToDictionary(e => e.EmployeeId, e => e);

            // Path sırasına göre sırala (en üst -> en alt) veya istersen tam tersi çevirebilirsin
            var result = new List<EmployeeAncestorDto>();
            foreach (var id in slice)
            {
                if (dict.TryGetValue(id, out var e))
                {
                    var level = pathIds.IndexOf(id) + 1; // 1-based
                    result.Add(ToAncestorDto(e, level));
                }
            }

            return result;
        }

        private static List<int> ParsePathIds(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new Exception("Path boş. Employee için Path üretilmemiş olabilir.");

            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            var ids = new List<int>(parts.Length);
            foreach (var p in parts)
            {
                if (!int.TryParse(p, out var id))
                    throw new Exception($"Path parse edilemedi: '{path}'");
                ids.Add(id);
            }

            if (ids.Count == 0)
                throw new Exception($"Path parse edilemedi: '{path}'");

            return ids;
        }

        private static EmployeeAncestorDto ToAncestorDto(Employee e, int level)
        {
            return new EmployeeAncestorDto
            {
                EmployeeId = e.EmployeeId,
                FullName = e.FullName,
                JobTitle = e.JobTitle,
                Department = e.Department,
                ManagerId = e.ManagerId,
                Path = e.Path,
                Level = level
            };
        }
    }
}
