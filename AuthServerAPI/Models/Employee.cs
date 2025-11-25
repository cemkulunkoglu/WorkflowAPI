using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthServerAPI.Models;

public class Employee
{
    [Key]
    public int EmployeeId { get; set; }

    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public User? User { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Department { get; set; }
}