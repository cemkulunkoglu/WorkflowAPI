using System.ComponentModel.DataAnnotations;

namespace AuthServerAPI.DTOs;

public class LoginDto
{
    [Required]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
