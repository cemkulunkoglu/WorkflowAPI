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
    }
}
