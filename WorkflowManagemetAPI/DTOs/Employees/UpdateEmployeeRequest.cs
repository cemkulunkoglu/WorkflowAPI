public class UpdateEmployeeRequest
{
    public int EmployeeId { get; set; }
    public int? UserId { get; set; }

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    public string? Phone { get; set; }
    public string? SicilNo { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }

    public int? ManagerId { get; set; }
}
