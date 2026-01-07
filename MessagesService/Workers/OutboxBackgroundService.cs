using Microsoft.EntityFrameworkCore;
using MessagesService.Data;
using MessagesService.Entities;
using MessagesService.Services;

namespace MessagesService.Workers;

public class OutboxBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxBackgroundService> _logger;

    private const int MaxRetry = 10;

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

                var now = DateTime.UtcNow;

                var pending = await db.Outbox
                    .Where(x => x.UpdateDate == null)
                    .Where(x => x.RetryCount < MaxRetry)
                    .Where(x => x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= now)
                    .OrderBy(x => x.CreateDate)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var msg in pending)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    try
                    {
                        await emailSender.SendAsync(msg.EmailFrom, msg.EmailTo, msg.Subject, msg.Body, stoppingToken);

                        msg.UpdateDate = DateTime.UtcNow;
                        msg.LastError = null;
                        msg.NextAttemptAtUtc = null;

                        // (İstersen inbox insert burada kalsın)
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
                            UpdateDate = null
                        };

                        db.Inbox.Add(inbox);

                        await db.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation("Outbox sent. OutboxId={Id} To={To}", msg.Id, msg.EmailTo);
                    }
                    catch (Exception ex)
                    {
                        msg.RetryCount += 1;
                        msg.LastError = ex.Message;

                        // backoff: 10s, 30s, 60s, 120s, 300s... (cap 10 dk)
                        var delaySeconds = msg.RetryCount switch
                        {
                            1 => 10,
                            2 => 30,
                            3 => 60,
                            4 => 120,
                            _ => 300
                        };

                        if (delaySeconds > 600) delaySeconds = 600;

                        msg.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(delaySeconds);

                        await db.SaveChangesAsync(stoppingToken);

                        _logger.LogError(ex, "Failed to send outbox. OutboxId={Id} Retry={Retry} Next={Next}",
                            msg.Id, msg.RetryCount, msg.NextAttemptAtUtc);
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
