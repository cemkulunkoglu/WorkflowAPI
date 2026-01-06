namespace WorkflowManagemetAPI.DTOs.LeaveRequests
{
    public class CreateLeaveRequestRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = "";
    }
}
