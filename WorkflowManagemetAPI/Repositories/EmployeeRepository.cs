using Microsoft.EntityFrameworkCore;
using WorkflowManagemetAPI.DbContext;
using WorkflowManagemetAPI.Interfaces;
using WorkflowManagemetAPI.Models;
using WorkflowManagemetAPI.UoW;

namespace WorkflowManagemetAPI.Repositories
{
    public class EmployeeRepository : UnitOfWork<Employee>, IEmployeeRepository
    {
        private readonly EmployeeDbContext _context;
        private readonly DbSet<Employee> _dbSet;

        public EmployeeRepository(EmployeeDbContext context)
            : base(context)
        {
            _context = context;
            _dbSet = context.Set<Employee>();
        }

        public IEnumerable<Employee> GetAll()
        {
            return _dbSet
                .AsNoTracking()
                .ToList();
        }

        public Employee? GetByEmployeeId(int employeeId)
        {
            return _dbSet
                .AsNoTracking()
                .FirstOrDefault(e => e.EmployeeId == employeeId);
        }

        public IEnumerable<Employee> GetByManagerId(int? managerId)
        {
            return _dbSet
                .AsNoTracking()
                .Where(e => e.ManagerId == managerId)
                .ToList();
        }

        public void AddEmployee(Employee employee)
        {
            Insert(employee);
        }

        public void UpdateEmployee(Employee employee)
        {
            Update(employee);
        }

        public void DeleteEmployee(Employee employee)
        {
            Delete(employee);
        }
    }
}
