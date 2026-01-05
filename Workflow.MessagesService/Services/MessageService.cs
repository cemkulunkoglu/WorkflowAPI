using Microsoft.EntityFrameworkCore;
using Workflow.MessagesService.DTOs;
using Workflow.MessagesService.Entities;
using Workflow.MessagesService.Persistence;

namespace Workflow.MessagesService.Services;

public class MessageService : IMessageService
{
    private readonly MessagesDbContext _dbContext;

    public MessageService(MessagesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> CreateOutboxAsync(SendMessageRequest request, CancellationToken cancellationToken)
    {
        var outboxMessage = new OutboxMessage
        {
            FlowDesignsId = request.FlowDesignsId,
            FlowNodesId = request.FlowNodesId,
            EmployeeToId = request.EmployeeToId,
            EmployeeFromId = request.EmployeeFromId,
            EmailTo = request.EmailTo,
            EmailFrom = request.EmailFrom,
            Subject = request.Subject,
            CreateDate = DateTime.UtcNow,
            UpdateDate = null
        };

        _dbContext.Outbox.Add(outboxMessage);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return outboxMessage.Id;
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetOutboxAsync(int employeeId, CancellationToken cancellationToken)
    {
        // Outbox mesajları gönderen kullanıcıya ait olduğu için EmployeeFromId üzerinden filtrelenir.
        return await _dbContext.Outbox
            .AsNoTracking()
            .Where(message => message.EmployeeFromId == employeeId)
            .OrderByDescending(message => message.CreateDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InboxMessage>> GetInboxAsync(int employeeId, CancellationToken cancellationToken)
    {
        return await _dbContext.Inbox
            .AsNoTracking()
            .Where(message => message.EmployeeToId == employeeId)
            .OrderByDescending(message => message.CreateDate)
            .ToListAsync(cancellationToken);
    }
}
