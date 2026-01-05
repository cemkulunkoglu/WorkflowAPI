using Microsoft.EntityFrameworkCore;
using MessagesService.Data;
using MessagesService.Entities;
using MessagesService.Services;

namespace MessagesService.Workers;

public class OutboxBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxBackgroundService> _logger;

    public OutboxBackgroundService(IServiceScopeFactory scopeFactory, ILogger<OutboxBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MessagesDbContext>();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                var pending = await db.Outbox
                    .Where(x => x.UpdateDate == null)
                    .OrderBy(x => x.CreateDate)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var msg in pending)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    try
                    {
                        var body =
                        $@"FlowDesignsId: {msg.FlowDesignsId}
                        FlowNodesId: {msg.FlowNodesId}
                        EmployeeFromId: {msg.EmployeeFromId}
                        EmployeeToId: {msg.EmployeeToId}

                        Subject: {msg.Subject}";

                        // 1) SMTP gönder
                        await emailSender.SendAsync(msg.EmailFrom, msg.EmailTo, msg.Subject, body, stoppingToken);

                        // 2) DB işlemleri (Outbox gönderildi + Inbox'a düş)
                        msg.UpdateDate = DateTime.UtcNow;

                        var inbox = new InboxMessage
                        {
                            FlowDesignsId = msg.FlowDesignsId,
                            FlowNodesId = msg.FlowNodesId,
                            EmployeeToId = msg.EmployeeToId,
                            EmployeeFromId = msg.EmployeeFromId,
                            EmailTo = msg.EmailTo,
                            EmailFrom = msg.EmailFrom,
                            Subject = msg.Subject,
                            CreateDate = DateTime.UtcNow,
                            UpdateDate = null // ✅ Inbox için: okunmadı
                        };

                        db.Inbox.Add(inbox);

                        await db.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send outbox message. OutboxId={Id}", msg.Id);
                        // UpdateDate null kalır => tekrar dener
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxBackgroundService loop error.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("OutboxBackgroundService stopped.");
    }
}
