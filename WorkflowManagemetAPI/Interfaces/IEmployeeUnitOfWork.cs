using Microsoft.EntityFrameworkCore.Storage;
using WorkflowManagemetAPI.Interfaces;

namespace WorkflowManagemetAPI.UnitOfWork
{
    public interface IEmployeeUnitOfWork : IDisposable
    {
        IEmployeeRepository Employees { get; }

        IDbContextTransaction BeginTransaction();
        void Commit(IDbContextTransaction tx);
        void Rollback(IDbContextTransaction tx);

        int SaveChanges();
        Task SaveChangesAsync();
    }
}
