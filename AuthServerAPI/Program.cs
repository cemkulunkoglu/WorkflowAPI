using AuthServerAPI.Data;
using AuthServerAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. DB Context Ayarı
var connectionString = builder.Configuration.GetConnectionString("AuthConnection");
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseMySQL(connectionString));

// 3. JWT Authentication Ayarları
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("isAdmin", "True"));
});

// 👇 4. CORS POLİTİKASI (EKSİK OLAN KISIM BURASIYDI) 👇
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173") // React uygulamanın adresi
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHttpClient("WorkflowApi", client =>
{
    var baseUrl = builder.Configuration["WorkflowApi:BaseUrl"];
    client.BaseAddress = new Uri(baseUrl!);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 👇 5. CORS MIDDLEWARE (MUTLAKA AUTHENTICATION'DAN ÖNCE OLMALI) 👇
app.UseCors("AllowFrontend");

app.UseAuthentication();

app.Use(async (context, next) =>
{
    // Swagger / public endpoint'leri bozmayalım
    var path = context.Request.Path.Value?.ToLower() ?? "";

    // Login/Register gibi AllowAnonymous endpoint'ler zaten auth istemiyor.
    // Ama token ile gelirse bile engellemeyelim.
    var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;

    if (isAuthenticated)
    {
        // Token içinden isVerified oku
        var isVerifiedClaim = context.User.FindFirst("isVerified")?.Value;

        // "True/False" ya da "true/false" gibi gelebilir
        var isVerified = string.Equals(isVerifiedClaim, "true", StringComparison.OrdinalIgnoreCase);

        // Verified değilse sadece change-password'a izin ver
        var isChangePasswordEndpoint =
            path.StartsWith("/api/account/change-password");

        if (!isVerified && !isChangePasswordEndpoint)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                message = "Hesabınız doğrulanmamış. Devam etmek için şifrenizi değiştirmeniz gerekiyor.",
                code = "ACCOUNT_NOT_VERIFIED"
            });

            return;
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllers();

app.Run();