using AuthServerAPI.Data;
using AuthServerAPI.DTOs;
using AuthServerAPI.Helpers;
using AuthServerAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Net.Http.Json;

namespace AuthServerAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthController(AuthDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
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
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            IsDesigner = true, // Varsayılan
            IsVerified = false
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        // Employee kaydı (Aynı kalıyor)
        var newEmployee = new Employee
        {
            UserId = newUser.UserId,
            FirstName = request.FullName.Split(' ')[0],
            LastName = request.FullName.Contains(' ')
                ? request.FullName.Substring(request.FullName.IndexOf(' ') + 1)
                : "",
            Department = "Genel"
        };

        _context.Employees.Add(newEmployee);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Kayıt başarılı." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        // 1) Email ya da username ile kullanıcıyı bul
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Email == request.UserNameOrEmail || u.UserName == request.UserNameOrEmail);

        if (user == null)
            return Unauthorized(new { message = "Geçersiz kullanıcı." });

        // 2) Şifre kontrolü
        if (!HashingHelper.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return Unauthorized(new { message = "Hatalı şifre." });
        }

        // 3) Kullanıcıya ait Employee bilgisini çek (Token için employeeId gerekiyor)
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.UserId);

        // İstersen sıkılaştır: employee yoksa login'i engelle
        if (employee == null)
        {
            return Unauthorized(new { message = "Personel kaydı bulunamadı. Lütfen yöneticinize başvurun." });
        }

        var fullName = $"{employee.FirstName} {employee.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
            fullName = user.UserName;

        var token = GenerateJwtToken(user, fullName, employee.EmployeeId);

        return Ok(new
        {
            token = token,
            userId = user.UserId.ToString(),
            employeeId = employee.EmployeeId, // response'a da eklemek istersen (opsiyonel ama faydalı)
            email = user.Email,
            fullName = fullName,
            isVerified = user.IsVerified
        });
    }

    private string GenerateJwtToken(User user, string fullName, int employeeId)
    {
        var claims = new List<Claim>
        {
            // UserId (sub) + NameIdentifier
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),

            // Email
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Email, user.Email),

            // ✅ EmployeeId claim (MessagesService ve frontend bununla çalışıyor)
            new Claim("employeeId", employeeId.ToString()),

            // Ek claims
            new Claim("fullName", fullName),
            new Claim("isDesigner", user.IsDesigner.ToString()),
            new Claim("isAdmin", user.IsDesigner.ToString()),
            new Claim("isVerified", user.IsVerified.ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(30),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("provision-employee")]
    public async Task<IActionResult> ProvisionEmployee([FromBody] CreateEmployeeUserDto request)
    {
        // 1) Username ve Email unique olsun
        if (await _context.Users.AnyAsync(u => u.UserName == request.UserName))
            return BadRequest(new { message = "Bu username zaten kayıtlı." });

        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest(new { message = "Bu email zaten kayıtlı." });

        // 2) Temp şifre
        var generatedPassword = PasswordGenerator.Generate(12);

        // 3) Hash + Salt
        HashingHelper.CreatePasswordHash(generatedPassword, out var passwordHash, out var passwordSalt);

        // 4) User oluştur
        var newUser = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            IsDesigner = false,
            IsVerified = false
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync(); // UserId burada oluşur

        // 5) WorkflowAPI'ye employee create at
        try
        {
            var client = _httpClientFactory.CreateClient("WorkflowApi");

            var workflowRequest = new
            {
                userId = newUser.UserId,
                firstName = request.FirstName,
                lastName = request.LastName,
                phone = request.Phone,
                sicilNo = request.SicilNo,
                jobTitle = request.JobTitle,
                department = request.Department,
                managerId = request.ManagerId
            };

            var wfResponse = await client.PostAsJsonAsync("/api/Employees/create", workflowRequest);

            if (!wfResponse.IsSuccessStatusCode)
            {
                // Rollback: user'ı sil
                _context.Users.Remove(newUser);
                await _context.SaveChangesAsync();

                var wfBody = await wfResponse.Content.ReadAsStringAsync();

                return StatusCode((int)wfResponse.StatusCode, new
                {
                    message = "Workflow employee create başarısız. User rollback edildi.",
                    workflowStatus = (int)wfResponse.StatusCode,
                    workflowBody = wfBody
                });
            }

            var wfEmp = await wfResponse.Content.ReadFromJsonAsync<WorkflowEmployeeResponseDto>();

            return Ok(new
            {
                message = "Provision başarılı (User + Workflow Employee).",
                userId = newUser.UserId,
                employeeId = wfEmp?.EmployeeId,
                path = wfEmp?.Path,
                userName = newUser.UserName,
                email = newUser.Email,
                temporaryPassword = generatedPassword,
                isVerified = newUser.IsVerified
            });
        }
        catch (Exception ex)
        {
            // Rollback: user'ı sil
            _context.Users.Remove(newUser);
            await _context.SaveChangesAsync();

            return StatusCode(500, new
            {
                message = "Workflow'a erişilemedi. User rollback edildi.",
                error = ex.Message
            });
        }
    }
}
