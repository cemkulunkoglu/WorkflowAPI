using AuthServerAPI.Data;
using AuthServerAPI.DTOs;
using AuthServerAPI.Helpers;
using AuthServerAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthServerAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AuthDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest(new { message = "Bu email adresi zaten kayıtlı." });
        }

        // Şifreyi hashliyoruz
        HashingHelper.CreatePasswordHash(request.Password, out string passwordHash, out string passwordSalt);

        var newUser = new User
        {
            UserName = request.Email,
            Email = request.Email,

            // Hashlenmiş verileri atıyoruz
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,

            IsDesigner = true // Varsayılan
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        // Employee kaydı (Aynı kalıyor)
        var newEmployee = new Employee
        {
            UserId = newUser.UserId,
            FirstName = request.FullName.Split(' ')[0],
            LastName = request.FullName.Contains(' ') ? request.FullName.Substring(request.FullName.IndexOf(' ') + 1) : "",
            Department = "Genel"
        };

        _context.Employees.Add(newEmployee);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Kayıt başarılı." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        // 1. Sadece Email ile kullanıcıyı bul (Şifre kontrolü sonra)
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            return Unauthorized(new { message = "Geçersiz email." });

        // 2. 👇 ŞİFRE KONTROLÜ: Helper sınıfını kullanıyoruz
        if (!HashingHelper.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return Unauthorized(new { message = "Hatalı şifre." });
        }

        // 3. Kullanıcıya ait Personel bilgisini çek (Token için)
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.UserId);
        string fullName = employee != null ? $"{employee.FirstName} {employee.LastName}" : user.UserName;

        var token = GenerateJwtToken(user, fullName);

        return Ok(new
        {
            token = token,
            userId = user.UserId.ToString(),
            email = user.Email,
            fullName = fullName
        });
    }

    private string GenerateJwtToken(User user, string fullName)
    {
        var claims = new List<Claim>
        {
            // Hem int ID hem String ID olarak ekleyelim
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),

            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("fullName", fullName),
            new Claim("isDesigner", user.IsDesigner.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,

            // DateTime.Now.AddHours(4) -> 4 Saat
            // DateTime.Now.AddDays(30) -> 30 Gün (Oturum 1 ay açık kalır)
            expires: DateTime.Now.AddDays(30),

            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}