using System.ComponentModel.DataAnnotations;

namespace AuthServerAPI.Models;

public class User
{
    [Key]
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public bool IsDesigner { get; set; }
    public bool IsVerified { get; set; } = false;
}