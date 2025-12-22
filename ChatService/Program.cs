using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Workflow.ChatService.Messaging;
using Workflow.ChatService.Streaming;
using Workflow.ChatService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<IAiService, GroqAiService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true);
        // SSE için credentials şart değilse kaldırmak daha temiz:
        // .AllowCredentials();
    });
});

// ✅ JWT (OIDC yok → secret ile doğrula)
var jwtSecret = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey tanımlı değil.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"];     // opsiyonel
var jwtAudience = builder.Configuration["Jwt:Audience"]; // opsiyonel

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),

            ValidateIssuer = false,
            ValidateAudience = false,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// ✅ SSE broker
builder.Services.AddSingleton<SseBroker>();

// ✅ RabbitMQ
builder.Services.AddSingleton<RabbitMqProducer>();
builder.Services.AddHostedService<RabbitMqConsumer>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
