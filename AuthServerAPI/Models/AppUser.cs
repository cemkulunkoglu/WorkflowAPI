using Microsoft.AspNetCore.Identity;

namespace AuthServerAPI.Models;

public class AppUser : IdentityUser
{
    // IdentityUser zaten Id, Email, PasswordHash, UserName içeriyor.
    // Biz ekstra alanlarımızı ekliyoruz.
    public string FullName { get; set; } = string.Empty;
}