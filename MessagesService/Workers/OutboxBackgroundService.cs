using Microsoft.EntityFrameworkCore;
using Workflow.MessagesService.Data;
using Workflow.MessagesService.Services;

namespace Workflow.MessagesService.Workers;

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

                // Küçük batch: 50 tane çekelim (istersen arttırırız)
                var pending = await db.Outbox
                    .Where(x => x.UpdateDate == null)
                    .OrderBy(x => x.CreateDate)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                if (pending.Count > 0)
                {
                    _logger.LogInformation("Outbox pending count: {Count}", pending.Count);
                }

                foreach (var msg in pending)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    try
                    {
                        // Body şimdilik basit bir içerik
                        var body =
                            $@"FlowDesignsId: {msg.FlowDesignsId}
                            FlowNodesId: {msg.FlowNodesId}
                            EmployeeFromId: {msg.EmployeeFromId}
                            EmployeeToId: {msg.EmployeeToId}

                            Subject: {msg.Subject}";

                        await emailSender.SendAsync(msg.EmailFrom, msg.EmailTo, msg.Subject, body, stoppingToken);

                        msg.UpdateDate = DateTime.UtcNow; // "gönderildi" işareti
                        await db.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        // Hata olursa UpdateDate set etmiyoruz => tekrar denenecek
                        _logger.LogError(ex, "Failed to send outbox message. OutboxId={Id}", msg.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxBackgroundService loop error.");
            }

            // Poll interval 
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("OutboxBackgroundService stopped.");
    }
}
