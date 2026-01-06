using WorkflowManagemetAPI.DTOs.LeaveRequests;

namespace WorkflowManagemetAPI.Interfaces.LeaveRequests
{
    public interface ILeaveRequestService
    {
        LeaveRequestResponseDto Create(CreateLeaveRequestRequest request);
    }
}
