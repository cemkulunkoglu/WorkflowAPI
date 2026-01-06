namespace WorkflowManagemetAPI.Events
{
    public class LeaveRequestCreatedEvent
    {
        public string EventType { get; set; } = "LeaveRequestCreated";
        public Guid EventId { get; set; } = Guid.NewGuid();
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public int LeaveRequestId { get; set; }
        public int EmployeeId { get; set; }
        public int ApproverEmployeeId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DayCount { get; set; }

        public string Reason { get; set; } = "";
    }
}
