using MessagesService.Data;
using MessagesService.Entities;
using MessagesService.Events;
using MessagesService.Interfaces;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace MessagesService.Workers;

public class LeaveRequestEventsConsumer : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IApproverEmailLookup _emailLookup;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LeaveRequestEventsConsumer> _logger;

    private RabbitMQ.Client.IConnection? _connection;
    private RabbitMQ.Client.IModel? _channel;
    private string _queueName = "";

    public LeaveRequestEventsConsumer(
        IConfiguration config,
        IApproverEmailLookup emailLookup,
        IServiceScopeFactory scopeFactory,
        ILogger<LeaveRequestEventsConsumer> logger)
    {
        _config = config;
        _emailLookup = emailLookup;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var exchange = _config["RabbitMQ:Exchange"] ?? "workflow.events";
        _queueName = _config["RabbitMQ:LeaveRequestsQueue"] ?? "workflow.messages.leave-requests";

        // DLQ/DLX settings
        var dlxExchange = "workflow.events.dlx";
        var dlRoutingKey = "leave-requests";
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

                if (eventName != "LeaveRequestCreated")
                {
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                var payloadJson = doc.RootElement.GetProperty("payload").GetRawText();
                var payload = JsonSerializer.Deserialize<LeaveRequestCreatedEvent>(payloadJson);

                if (payload == null)
                    throw new Exception("LeaveRequestCreated payload parse failed.");

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MessagesDbContext>();

                // ✅ Config: kaç gün ve üzeri 2 üst mail alsın?
                var secondApproverMinDays = int.TryParse(_config["LeaveRequest:SecondApproverMinDays"], out var d) ? d : 4;

                // ✅ Employee.Path üzerinden alıcıları çıkar
                var path = await _emailLookup.GetEmployeePathByEmployeeIdAsync(payload.EmployeeId, stoppingToken);

                if (string.IsNullOrWhiteSpace(path))
                {
                    _logger.LogWarning("[LeaveRequestCreated] Path not found. EmployeeId={EmployeeId}", payload.EmployeeId);
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                var pathIds = ParsePathIds(path);
                if (pathIds.Count < 2)
                {
                    _logger.LogWarning(
                        "[LeaveRequestCreated] Path does not include manager. EmployeeId={EmployeeId} Path={Path}",
                        payload.EmployeeId, path);

                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                // path: [..., managerId, employeeId]
                var manager1Id = pathIds[^2];
                int? manager2Id = pathIds.Count >= 3 ? pathIds[^3] : null;

                List<int> recipients;

                if (payload.DayCount >= secondApproverMinDays && manager2Id.HasValue)
                {
                    // 4 gün ve üzeri → SADECE 2. yönetici
                    recipients = new List<int> { manager2Id.Value };
                }
                else
                {
                    // 1–3 gün → SADECE 1. yönetici
                    recipients = new List<int> { manager1Id };
                }

                // distinct + sender hariç
                recipients = recipients
                    .Where(x => x > 0 && x != payload.EmployeeId)
                    .Distinct()
                    .ToList();

                if (recipients.Count == 0)
                {
                    _logger.LogWarning(
                        "[LeaveRequestCreated] No recipients resolved. EmployeeId={EmployeeId} Path={Path}",
                        payload.EmployeeId, path);

                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                _logger.LogInformation(
                    "[LeaveRequestCreated] Recipients resolved. LeaveRequestId={LeaveRequestId} EmployeeId={EmployeeId} DayCount={DayCount} Recipients={Recipients}",
                    payload.LeaveRequestId, payload.EmployeeId, payload.DayCount, string.Join(",", recipients));

                // ✅ Template hazırlığı (bir kere)
                var templateName = _config["LeaveRequest:TemplateName"] ?? "LEAVE_REQUEST_CREATED";

                var template = await db.MessageTemplates
                    .FirstOrDefaultAsync(t => t.Name == templateName && t.IsActive, stoppingToken);

                if (template != null)
                    _logger.LogInformation("[LeaveRequestCreated] Template found. Name={TemplateName}", templateName);
                else
                    _logger.LogWarning("[LeaveRequestCreated] Template NOT found. Using fallback. Name={TemplateName}", templateName);

                // isimler (employee)
                var employeeName = await _emailLookup
                    .GetEmployeeFullNameByEmployeeIdAsync(payload.EmployeeId, stoppingToken)
                    ?? $"Employee#{payload.EmployeeId}";

                // URL'ler
                var baseUrl = _config["Frontend:BaseUrl"] ?? "http://localhost:5173";
                var inboxUrl = $"{baseUrl.TrimEnd('/')}/dashboard?tab=messages&box=inbox";

                // UI template Onayla / Reddet kullanıyorsa diye:
                // (şimdilik aynı sayfaya yönlendiriyoruz; ileride action param ekleyebilirsin)
                var approveUrl = inboxUrl;
                var rejectUrl = inboxUrl;

                var from = _config["Smtp:From"] ?? "noreply@workflow.local";

                var anyInserted = false;

                foreach (var toEmployeeId in recipients)
                {
                    // ✅ Idempotency per recipient
                    var messageId = $"{payload.LeaveRequestId}:{toEmployeeId}";

                    var alreadyProcessed = await db.ProcessedMessages
                        .AnyAsync(x => x.MessageId == messageId, stoppingToken);

                    if (alreadyProcessed)
                    {
                        _logger.LogInformation(
                            "[LeaveRequestCreated] Already processed for recipient. MessageId={MessageId}",
                            messageId);

                        continue;
                    }

                    // email + approver name
                    var toEmail = await _emailLookup.GetEmployeeEmailByEmployeeIdAsync(toEmployeeId, stoppingToken);
                    if (string.IsNullOrWhiteSpace(toEmail))
                    {
                        _logger.LogWarning(
                            "[LeaveRequestCreated] Recipient email not found. ToEmployeeId={ToEmployeeId}",
                            toEmployeeId);

                        continue;
                    }

                    var approverName = await _emailLookup
                        .GetEmployeeFullNameByEmployeeIdAsync(toEmployeeId, stoppingToken)
                        ?? $"Approver#{toEmployeeId}";

                    // ----------------------------
                    // subject/body + uiBody render
                    // ----------------------------
                    string subject;
                    string body;
                    string uiBody;

                    if (template != null)
                    {
                        subject = template.Subject ?? "Yeni İzin Talebi";
                        body = template.Body ?? "";

                        // ✅ UI: Option B
                        // UI body boşsa email body fallback (sağlam)
                        uiBody = (template.UiBody ?? template.Body ?? "");

                        // subject
                        subject = subject.Replace("{user_name}", employeeName);

                        // email body
                        body = body
                            .Replace("{user_name}", employeeName)
                            .Replace("{approver_name}", approverName)
                            .Replace("{start_date}", payload.StartDate.ToString("dd.MM.yyyy"))
                            .Replace("{end_date}", payload.EndDate.ToString("dd.MM.yyyy"))
                            .Replace("{day_count}", payload.DayCount.ToString())
                            .Replace("{reason}", payload.Reason ?? "")
                            .Replace("{leave_request_id}", payload.LeaveRequestId.ToString())
                            .Replace("{approve_url}", approveUrl)
                            .Replace("{reject_url}", rejectUrl);

                        // ui body
                        uiBody = uiBody
                            .Replace("{user_name}", employeeName)
                            .Replace("{approver_name}", approverName)
                            .Replace("{start_date}", payload.StartDate.ToString("dd.MM.yyyy"))
                            .Replace("{end_date}", payload.EndDate.ToString("dd.MM.yyyy"))
                            .Replace("{day_count}", payload.DayCount.ToString())
                            .Replace("{reason}", payload.Reason ?? "")
                            .Replace("{leave_request_id}", payload.LeaveRequestId.ToString())
                            .Replace("{approve_url}", approveUrl)
                            .Replace("{reject_url}", rejectUrl);
                    }
                    else
                    {
                        subject = "Yeni İzin Talebi";
                        body =
                            $"Merhaba {approverName},\n\n" +
                            $"{employeeName} tarafından izin talebi oluşturuldu.\n" +
                            $"Tarih: {payload.StartDate:dd.MM.yyyy} - {payload.EndDate:dd.MM.yyyy}\n" +
                            $"Gün: {payload.DayCount}\n" +
                            $"Sebep: {payload.Reason ?? ""}\n\n" +
                            $"Talep No: {payload.LeaveRequestId}\n" +
                            $"Detay: {inboxUrl}\n";

                        // UI fallback: sade bir içerik
                        uiBody =
                            $"<div style=\"font-family: Arial, sans-serif; color:#111827;\">" +
                            $"<h3 style=\"margin:0 0 8px; font-size:18px;\">Yeni İzin Talebi</h3>" +
                            $"<p style=\"margin:0 0 12px; font-size:14px; line-height:20px;\">" +
                            $"<strong>{employeeName}</strong> tarafından " +
                            $"<strong>{payload.StartDate:dd.MM.yyyy}</strong> – <strong>{payload.EndDate:dd.MM.yyyy}</strong> " +
                            $"tarihleri arasında (<strong>{payload.DayCount}</strong> gün) izin talebi oluşturuldu.</p>" +
                            $"<div style=\"background:#f9fafb; border:1px solid #e5e7eb; border-radius:10px; padding:12px;\">" +
                            $"<strong>Sebep:</strong><br/>{System.Net.WebUtility.HtmlEncode(payload.Reason ?? "")}</div>" +
                            $"</div>";
                    }

                    // ✅ Outbox insert
                    var outbox = new OutboxMessage
                    {
                        FlowDesignsId = 0,
                        FlowNodesId = 0,
                        EmployeeFromId = payload.EmployeeId,
                        EmployeeToId = toEmployeeId,
                        EmailFrom = from,
                        EmailTo = toEmail,
                        Subject = subject,
                        Body = body,

                        // 🔥 KRİTİK: UI içeriği DB'ye yaz
                        UiBody = uiBody,

                        CreateDate = DateTime.UtcNow,
                        UpdateDate = null,
                        RetryCount = 0,
                        NextAttemptAtUtc = null,
                        LastError = null
                    };

                    db.Outbox.Add(outbox);

                    // ✅ ProcessedMessages insert (per recipient)
                    db.ProcessedMessages.Add(new ProcessedMessage
                    {
                        MessageId = messageId,
                        EventName = "LeaveRequestCreated",
                        ProcessedAtUtc = DateTime.UtcNow,
                        Status = "Processed",
                        LastError = null
                    });

                    anyInserted = true;

                    _logger.LogInformation(
                        "[LeaveRequestCreated] Outbox queued. LeaveRequestId={LeaveRequestId} ToEmployeeId={ToEmployeeId} ToEmail={ToEmail} MessageId={MessageId}",
                        payload.LeaveRequestId, toEmployeeId, toEmail, messageId);
                }

                if (anyInserted)
                    await db.SaveChangesAsync(stoppingToken);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);

                _logger.LogInformation(
                    "[LeaveRequestCreated] Completed. LeaveRequestId={LeaveRequestId} Inserted={InsertedCount}",
                    payload.LeaveRequestId, anyInserted ? "YES" : "NO (already processed / missing emails)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ERROR] LeaveRequestEventsConsumer failed.");

                // ✅ requeue:false => DLQ
                _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
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

    private static List<int> ParsePathIds(string path)
    {
        return path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();
    }
}
