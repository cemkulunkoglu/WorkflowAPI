namespace WorkflowManagemetAPI.DTOs.Employees
{
    public class EmployeeAncestorDto
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = "";
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public int? ManagerId { get; set; }
        public string? Path { get; set; }
        public int Level { get; set; }
    }
}
