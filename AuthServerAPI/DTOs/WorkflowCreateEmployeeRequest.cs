namespace AuthServerAPI.DTOs;

public class WorkflowEmployeeResponseDto
{
    public int EmployeeId { get; set; }
    public int? UserId { get; set; }
    public string FullName { get; set; } = "";
    public string? Path { get; set; }
}
