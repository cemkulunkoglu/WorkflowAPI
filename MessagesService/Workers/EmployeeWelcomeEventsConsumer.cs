using MessagesService.Data;
using MessagesService.Entities;
using MessagesService.Events;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace MessagesService.Workers;

public class EmployeeWelcomeEventsConsumer : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmployeeWelcomeEventsConsumer> _logger;

    private RabbitMQ.Client.IConnection? _connection;
    private RabbitMQ.Client.IModel? _channel;
    private string _queueName = "";

    public EmployeeWelcomeEventsConsumer(
        IConfiguration config,
        IServiceScopeFactory scopeFactory,
        ILogger<EmployeeWelcomeEventsConsumer> logger)
    {
        _config = config;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var exchange = _config["RabbitMQ:Exchange"] ?? "workflow.events";
        _queueName = _config["RabbitMQ:EmployeeWelcomeQueue"] ?? "workflow.messages.employee-welcome";

        // DLQ/DLX settings
        var dlxExchange = "workflow.events.dlx";
        var dlRoutingKey = "employee-welcome";
        var dlqName = _queueName + ".dlq";

        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMQ:HostName"] ?? "localhost",
            UserName = _config["RabbitMQ:UserName"] ?? "guest",
            Password = _config["RabbitMQ:Password"] ?? "guest",
            Port = int.TryParse(_config["RabbitMQ:Port"], out var p) ? p : 5672,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Main exchange
        _channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout, durable: true);

        // DLX + DLQ
        _channel.ExchangeDeclare(exchange: dlxExchange, type: ExchangeType.Direct, durable: true);
        _channel.QueueDeclare(queue: dlqName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        _channel.QueueBind(queue: dlqName, exchange: dlxExchange, routingKey: dlRoutingKey);

        // Main queue with DLX args
        var args = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = dlxExchange,
            ["x-dead-letter-routing-key"] = dlRoutingKey
        };

        _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
        _channel.QueueBind(queue: _queueName, exchange: exchange, routingKey: "");

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        _logger.LogInformation(
            "RabbitMQ consumer init OK. Exchange={Exchange} Queue={Queue} DLX={DLX} DLQ={DLQ} Host={Host}:{Port}",
            exchange, _queueName, dlxExchange, dlqName, factory.HostName, factory.Port);

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel == null)
            throw new Exception("RabbitMQ channel not initialized.");

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                using var doc = JsonDocument.Parse(json);
                var eventName = doc.RootElement.GetProperty("eventName").GetString();

                if (eventName != "EmployeeWelcomeRequested")
                {
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                var payloadJson = doc.RootElement.GetProperty("payload").GetRawText();
                var payload = JsonSerializer.Deserialize<EmployeeWelcomeRequestedEvent>(payloadJson);

                if (payload == null)
                    throw new Exception("EmployeeWelcomeRequested payload parse failed.");

                // ✅ Idempotency: aynı kullanıcıya 1 kere
                var messageId = $"welcome:{payload.UserId}";

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MessagesDbContext>();

                var alreadyProcessed = await db.ProcessedMessages
                    .AnyAsync(x => x.MessageId == messageId, stoppingToken);

                if (alreadyProcessed)
                {
                    _logger.LogInformation("[EmployeeWelcomeRequested] Already processed. MessageId={MessageId}", messageId);
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                // ✅ Template
                var templateName = _config["EmployeeWelcome:TemplateName"] ?? "EMPLOYEE_WELCOME_PASSWORD";

                var template = await db.MessageTemplates
                    .FirstOrDefaultAsync(t => t.Name == templateName && t.IsActive, stoppingToken);

                var companyName = _config["Company:Name"] ?? "Workflow";
                var baseUrl = _config["Frontend:BaseUrl"] ?? "";
                var loginUrl = string.IsNullOrWhiteSpace(baseUrl) ? "" : $"{baseUrl.TrimEnd('/')}/login";

                var from = _config["Smtp:From"] ?? "noreply@workflow.local";

                var fullName = string.IsNullOrWhiteSpace(payload.FullName) ? payload.UserName : payload.FullName;

                string subject;
                string body;

                if (template != null)
                {
                    subject = template.Subject ?? "Hesabınız Oluşturuldu";
                    body = template.Body ?? "";

                    subject = subject
                        .Replace("{company_name}", companyName)
                        .Replace("{user_name}", fullName)
                        .Replace("{email}", payload.Email);

                    body = body
                        .Replace("{company_name}", companyName)
                        .Replace("{user_name}", fullName)
                        .Replace("{email}", payload.Email)
                        .Replace("{temporary_password}", payload.TemporaryPassword)
                        .Replace("{login_url}", loginUrl);
                }
                else
                {
                    subject = "Hesabınız Oluşturuldu";
                    body =
                        $"Merhaba {fullName},\n\n" +
                        $"Şirket: {companyName}\n" +
                        $"Email: {payload.Email}\n" +
                        $"Geçici Şifre: {payload.TemporaryPassword}\n" +
                        $"Giriş: {loginUrl}\n";
                }

                // ✅ Outbox insert
                db.Outbox.Add(new OutboxMessage
                {
                    FlowDesignsId = 0,
                    FlowNodesId = 0,
                    EmployeeFromId = 0, // sistem
                    EmployeeToId = payload.EmployeeId,

                    EmailFrom = from,
                    EmailTo = payload.Email,
                    Subject = subject,
                    Body = body,

                    CreateDate = DateTime.UtcNow,
                    UpdateDate = null,
                    RetryCount = 0,
                    NextAttemptAtUtc = null,
                    LastError = null
                });

                // ✅ ProcessedMessages insert
                db.ProcessedMessages.Add(new ProcessedMessage
                {
                    MessageId = messageId,
                    EventName = "EmployeeWelcomeRequested",
                    ProcessedAtUtc = DateTime.UtcNow,
                    Status = "Processed",
                    LastError = null
                });

                await db.SaveChangesAsync(stoppingToken);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);

                _logger.LogInformation(
                    "[EmployeeWelcomeRequested] Outbox queued. ToEmail={ToEmail} UserId={UserId} EmployeeId={EmployeeId} MessageId={MessageId}",
                    payload.Email, payload.UserId, payload.EmployeeId, messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ERROR] EmployeeWelcomeEventsConsumer failed.");
                _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false); // DLQ
            }
        };

        _logger.LogInformation("Consuming started. Queue={Queue}", _queueName);

        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try { _channel?.Close(); } catch { }
        try { _connection?.Close(); } catch { }
        _channel?.Dispose();
        _connection?.Dispose();

        return base.StopAsync(cancellationToken);
    }
}
