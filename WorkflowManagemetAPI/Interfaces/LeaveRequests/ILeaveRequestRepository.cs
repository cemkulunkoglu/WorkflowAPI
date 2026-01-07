using WorkflowManagemetAPI.Entities;

namespace WorkflowManagemetAPI.Interfaces.LeaveRequests
{
    public interface ILeaveRequestRepository
    {
        void Add(LeaveRequest entity);

        List<LeaveRequest> GetByEmployeeId(int employeeId);
    }
}
