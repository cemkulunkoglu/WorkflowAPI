using AuthServerAPI.Data;
using AuthServerAPI.DTOs;
using AuthServerAPI.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthServerAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly AuthDbContext _context;

    public AccountController(AuthDbContext context)
    {
        _context = context;
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
    {
        // 1) Token içinden userId al
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Token geçersiz (userId bulunamadı)." });

        // 2) Kullanıcıyı DB’den çek
        var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == userId);
        if (user == null)
            return Unauthorized(new { message = "Kullanıcı bulunamadı." });

        // 3) Mevcut şifre doğru mu?
        var isCurrentOk = HashingHelper.VerifyPasswordHash(
            request.CurrentPassword,
            user.PasswordHash,
            user.PasswordSalt
        );

        if (!isCurrentOk)
            return BadRequest(new { message = "Mevcut şifre hatalı." });

        // 4) Yeni şifreyi hashle ve kaydet
        HashingHelper.CreatePasswordHash(request.NewPassword, out var newHash, out var newSalt);

        user.PasswordHash = newHash;
        user.PasswordSalt = newSalt;

        // Şifre değişti → kullanıcı artık verified
        user.IsVerified = true;

        await _context.SaveChangesAsync();

        // ❗ Yeni token üretmiyoruz
        // ❗ Kullanıcıya açıkça yeniden login zorunlu diyoruz
        return Ok(new
        {
            message = "Şifre başarıyla güncellendi. Güvenlik nedeniyle yeniden giriş yapılması gerekiyor.",
            forceReLogin = true
        });
    }
}
