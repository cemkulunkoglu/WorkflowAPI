using MessagesService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Workflow.MessagesService.Data;
using Workflow.MessagesService.Services;
using Workflow.MessagesService.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT
var jwt = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwt["SecretKey"];
var issuer = jwt["Issuer"];
var audience = jwt["Audience"];

if (string.IsNullOrWhiteSpace(secretKey) ||
    string.IsNullOrWhiteSpace(issuer) ||
    string.IsNullOrWhiteSpace(audience))
{
    throw new InvalidOperationException("JwtSettings eksik. appsettings.json içinde SecretKey/Issuer/Audience olmalý.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,

            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),

            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// DbContext
var connStr = builder.Configuration.GetConnectionString("MessagesDB");
if (string.IsNullOrWhiteSpace(connStr))
{
    throw new InvalidOperationException("ConnectionStrings:MessagesDB eksik. appsettings.json içine eklemelisin.");
}

builder.Services.AddDbContext<MessagesDbContext>(opt =>
    opt.UseMySql(connStr, ServerVersion.AutoDetect(connStr)));

// SMTP Options + Sender
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// Application services
builder.Services.AddScoped<IMessageService, MessageService>();

// Background worker
builder.Services.AddHostedService<OutboxBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
