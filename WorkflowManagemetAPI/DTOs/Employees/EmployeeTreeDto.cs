namespace WorkflowManagemetAPI.DTOs.Employees
{
    public class EmployeeTreeDto
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public int? ManagerId { get; set; }
        public string? Path { get; set; }

        public List<EmployeeTreeDto> Children { get; set; } = new();
    }
}
