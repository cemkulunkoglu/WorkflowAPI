namespace WorkflowManagemetAPI.DTOs.LeaveRequests
{
    public class LeaveRequestResponseDto
    {
        public int LeaveRequestId { get; set; }
        public string Status { get; set; } = "Pending";
        public int ApproverEmployeeId { get; set; }
        public int DayCount { get; set; }
    }
}
