using Microsoft.EntityFrameworkCore.Storage;
using WorkflowManagemetAPI.DbContext;
using WorkflowManagemetAPI.Interfaces.Employees;
using WorkflowManagemetAPI.Interfaces.LeaveRequests;
using WorkflowManagemetAPI.Interfaces.UnitOfWork;
using WorkflowManagemetAPI.Repositories.Employees;
using WorkflowManagemetAPI.Repositories.LeaveRequests;

namespace WorkflowManagemetAPI.UnitOfWork
{
    public class EmployeeUnitOfWork : IEmployeeUnitOfWork
    {
        private readonly EmployeeDbContext _context;

        public IEmployeeRepository Employees { get; }
        public ILeaveRequestRepository LeaveRequests { get; private set; }

        public EmployeeUnitOfWork(EmployeeDbContext context)
        {
            _context = context;
            Employees = new EmployeeRepository(_context);
            LeaveRequests = new LeaveRequestRepository(_context);
        }

        public IDbContextTransaction BeginTransaction() => _context.Database.BeginTransaction();

        public void Commit(IDbContextTransaction tx)
        {
            try { tx.Commit(); }
            catch { Rollback(tx); throw; }
            finally { tx.Dispose(); }
        }

        public void Rollback(IDbContextTransaction tx)
        {
            tx.Rollback();
            tx.Dispose();
        }

        public int SaveChanges() => _context.SaveChanges();
        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public void Dispose() => _context.Dispose();
    }
}
