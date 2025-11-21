using AuthServerAPI.DTOs;
using AuthServerAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthServerAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthController(UserManager<AppUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    // 🟢 KAYIT OL (REGISTER)
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        // 1. Kullanıcı var mı kontrol et
        var userExists = await _userManager.FindByEmailAsync(request.Email);
        if (userExists != null)
            return BadRequest(new { message = "Bu email adresi zaten kayıtlı." });

        // 2. Yeni kullanıcı nesnesi oluştur
        var newUser = new AppUser
        {
            UserName = request.Email, // Genelde UserName yerine Email kullanılır
            Email = request.Email,
            FullName = request.FullName,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        // 3. Kullanıcıyı kaydet (Şifreyi otomatik hash'ler)
        var result = await _userManager.CreateAsync(newUser, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { message = "Kayıt oluşturulamadı.", errors = errors });
        }

        return Ok(new { message = "Kullanıcı başarıyla oluşturuldu." });
    }

    // 🔵 GİRİŞ YAP (LOGIN) -> TOKEN ÜRET
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        // 1. Kullanıcıyı bul
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Unauthorized(new { message = "Geçersiz email veya şifre." });

        // 2. Şifreyi kontrol et
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
            return Unauthorized(new { message = "Geçersiz email veya şifre." });

        // 3. TOKEN OLUŞTURMA (JWT)
        var token = GenerateJwtToken(user);

        // 4. Sonucu dön
        return Ok(new
        {
            token = token,
            userId = user.Id,
            email = user.Email,
            fullName = user.FullName
        });
    }

    // 🔐 JWT Üretme Metodu (Helper)
    private string GenerateJwtToken(AppUser user)
    {
        // Token'ın içine gömeceğimiz bilgiler (Claims)
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id), // Kullanıcı ID'si
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("fullName", user.FullName) // Özel claim (Frontend'de göstermek için)
        };

        // Gizli Anahtarı al
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Token ayarları
        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(4), // Token 4 saat geçerli
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}