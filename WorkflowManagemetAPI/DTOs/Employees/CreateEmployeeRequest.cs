namespace WorkflowManagemetAPI.DTOs.Employees
{
    public class CreateEmployeeRequest
    {
        public int? UserId { get; set; }        // Opsiyonel (login hesabı)
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string? Phone { get; set; }
        public string? SicilNo { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }

        public int? ManagerId { get; set; }     // Üst çalışan
    }
}
