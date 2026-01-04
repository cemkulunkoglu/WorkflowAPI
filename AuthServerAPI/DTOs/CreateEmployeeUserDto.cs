using System.ComponentModel.DataAnnotations;

namespace AuthServerAPI.DTOs;

public class CreateEmployeeUserDto
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public string? SicilNo { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }

    public int? ManagerId { get; set; }
}
