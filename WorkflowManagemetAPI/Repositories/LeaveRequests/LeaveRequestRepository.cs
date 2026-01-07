using Microsoft.EntityFrameworkCore;
using WorkflowManagemetAPI.DbContext;
using WorkflowManagemetAPI.Entities;
using WorkflowManagemetAPI.Interfaces.LeaveRequests;

namespace WorkflowManagemetAPI.Repositories.LeaveRequests
{
    public class LeaveRequestRepository : ILeaveRequestRepository
    {
        private readonly EmployeeDbContext _context;

        public LeaveRequestRepository(EmployeeDbContext context)
        {
            _context = context;
        }

        public void Add(LeaveRequest entity)
        {
            _context.LeaveRequests.Add(entity);
        }

        public List<LeaveRequest> GetByEmployeeId(int employeeId)
        {
            return _context.LeaveRequests
                .AsNoTracking()
                .Where(x => x.EmployeeId == employeeId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToList();
        }
    }
}
