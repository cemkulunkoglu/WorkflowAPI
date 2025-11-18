using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WorkflowManagemetAPI.DbContext;
using WorkflowManagemetAPI.Interfaces;
using WorkflowManagemetAPI.Repositories;
using WorkflowManagemetAPI.Services;
using WorkflowManagemetAPI.UoW;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// 🔧 Controller + JSON ayarları
// ----------------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

// ----------------------------
// 📘 Swagger
// ----------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



builder.Services.AddDbContext<WorkflowFlowDbContext>(options =>
{
	options.UseMySQL(builder.Configuration.GetConnectionString("WorkflowDB"),
			 mySqlOptions => mySqlOptions.EnableRetryOnFailure());
});


// ----------------------------
// 🧩 Servis bağımlılıkları
// ----------------------------


//Repositories
builder.Services.AddScoped<IFlowDesignRepository, FlowDesignRepository>();
builder.Services.AddScoped<IFlowNodeRepository, FlowNodeRepository>();


//Context
builder.Services.AddScoped<DbContext, WorkflowFlowDbContext>();

// Service DI
builder.Services.AddScoped<IDesignService, DesignService>();

// ----------------------------
// 🌐 CORS
// ----------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ----------------------------
// 🚀 Pipeline
// ----------------------------
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("FrontendCors");
app.UseAuthorization();
app.MapControllers();
app.Run();
