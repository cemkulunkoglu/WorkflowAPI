using Microsoft.EntityFrameworkCore;
using Workflow.MessagesService.Persistence;

namespace Workflow.MessagesService.Services;

public class OutboxBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<OutboxBackgroundService> _logger;

    public OutboxBackgroundService(
        IServiceScopeFactory scopeFactory,
        IEmailSender emailSender,
        ILogger<OutboxBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _emailSender = emailSender;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessOutboxAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessOutboxAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessagesDbContext>();

        var pendingMessages = await dbContext.Outbox
            .Where(message => message.UpdateDate == null)
            .OrderBy(message => message.CreateDate)
            .Take(20)
            .ToListAsync(stoppingToken);

        if (pendingMessages.Count == 0)
        {
            return;
        }

        foreach (var message in pendingMessages)
        {
            try
            {
                await _emailSender.SendAsync(message, stoppingToken);
                message.UpdateDate = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send outbox message {OutboxId}", message.Id);
            }
        }
    }
}
