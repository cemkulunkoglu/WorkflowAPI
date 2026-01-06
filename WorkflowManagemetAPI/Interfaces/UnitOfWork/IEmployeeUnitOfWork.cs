using Microsoft.EntityFrameworkCore.Storage;
using WorkflowManagemetAPI.Interfaces.Employees;
using WorkflowManagemetAPI.Interfaces.LeaveRequests;

namespace WorkflowManagemetAPI.Interfaces.UnitOfWork
{
    public interface IEmployeeUnitOfWork : IDisposable
    {
        IEmployeeRepository Employees { get; }
        IDbContextTransaction BeginTransaction();
        void Commit(IDbContextTransaction tx);
        void Rollback(IDbContextTransaction tx);
        ILeaveRequestRepository LeaveRequests { get; }
        int SaveChanges();
        Task SaveChangesAsync();

    }
}
