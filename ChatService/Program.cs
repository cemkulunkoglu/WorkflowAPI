using Workflow.ChatService.Hubs;
using Workflow.ChatService.Messaging;
using Workflow.ChatService.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true)
              .AllowCredentials();
    });
});

// ✅ SignalR
builder.Services.AddSignalR();

// ✅ RAM Store
builder.Services.AddSingleton<IChatStore, InMemoryChatStore>();

// ✅ RabbitMQ Producer + Consumer
builder.Services.AddSingleton<RabbitMqProducer>();
builder.Services.AddHostedService<RabbitMqConsumer>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseStaticFiles();

app.MapControllers();

// ✅ Hub endpoint
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
