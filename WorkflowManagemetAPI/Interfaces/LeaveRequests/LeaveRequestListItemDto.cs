namespace WorkflowManagemetAPI.DTOs.LeaveRequests
{
    public class LeaveRequestListItemDto
    {
        public int LeaveRequestId { get; set; }
        public int EmployeeId { get; set; }
        public int ApproverEmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DayCount { get; set; }
        public string Reason { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAtUtc { get; set; }
    }
}
