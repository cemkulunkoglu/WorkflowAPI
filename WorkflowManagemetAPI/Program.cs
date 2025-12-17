using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using WorkflowManagemetAPI.DbContext;
using WorkflowManagemetAPI.Interfaces;
using WorkflowManagemetAPI.Repositories;
using WorkflowManagemetAPI.Services;
using WorkflowManagemetAPI.UoW;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// 🔧 1. Controller + JSON Ayarları
// ----------------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

// ----------------------------
// 📘 2. Swagger Ayarları (Kilit Butonu Ekli)
// ----------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Workflow API", Version = "v1" });

    // Swagger arayüzüne "Authorize" butonu ekliyoruz
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Örnek: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// ----------------------------
// 🔐 3. JWT Authentication Ayarları
// ----------------------------
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

// ----------------------------
// 🗄️ 4. Veritabanı Bağlantısı
// ----------------------------
builder.Services.AddDbContext<WorkflowFlowDbContext>(options =>
{
    options.UseMySQL(builder.Configuration.GetConnectionString("WorkflowDB"));
});

builder.Services.AddDbContext<EmployeeDbContext>(options =>
{
    options.UseMySQL(builder.Configuration.GetConnectionString("EmployeeDB"));
});

// ----------------------------
// 🧩 5. Servis Bağımlılıkları (DI)
// ----------------------------

// HttpContext'e erişmek için (Token içinden User ID okumak için şart)
builder.Services.AddHttpContextAccessor();

// Repositories
builder.Services.AddScoped<IFlowDesignRepository, FlowDesignRepository>();
builder.Services.AddScoped<IFlowNodeRepository, FlowNodeRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

// Context
builder.Services.AddScoped<DbContext, WorkflowFlowDbContext>();

// Services
builder.Services.AddScoped<IDesignService, DesignService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// ----------------------------
// 🌐 6. CORS Ayarları
// ----------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ----------------------------
// 🚀 7. Pipeline (Çalışma Sırası)
// ----------------------------
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("FrontendCors");

// DİKKAT: Authentication (Kimlik Sorma) her zaman Authorization (Yetki Sorma)'dan önce gelmelidir!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();