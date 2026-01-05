using Microsoft.EntityFrameworkCore;
using Workflow.MessagesService.Persistence;
using Workflow.MessagesService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MessagesDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("MessagesDb")));

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddHostedService<OutboxBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MessagesDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
