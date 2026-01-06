using WorkflowManagemetAPI.Entities;

namespace WorkflowManagemetAPI.Interfaces.LeaveRequests
{
    public interface ILeaveRequestRepository
    {
        void Add(LeaveRequest entity);
    }
}
