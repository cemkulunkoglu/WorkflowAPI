using MessagesService.Data;
using MessagesService.Dtos;
using MessagesService.Entities;
using Microsoft.EntityFrameworkCore;
using MessagesService.Data;
using MessagesService.Dtos;
using MessagesService.Entities;
using MessagesService.Services;

namespace MessagesService.Services;

public class MessageService : IMessageService
{
    private readonly MessagesDbContext _db;

    public MessageService(MessagesDbContext db)
    {
        _db = db;
    }

    public async Task<int> SendAsync(SendMessageRequest request, int employeeFromId, string emailFrom,CancellationToken ct)
    {
        var outbox = new OutboxMessage
        {
            FlowDesignsId = request.FlowDesignsId,
            FlowNodesId = request.FlowNodesId,
            EmployeeToId = request.EmployeeToId,
            EmployeeFromId = employeeFromId,
            EmailTo = request.EmailTo,
            EmailFrom = emailFrom,
            Subject = request.Subject,
            CreateDate = DateTime.UtcNow,
            UpdateDate = null
        };

        _db.Outbox.Add(outbox);
        await _db.SaveChangesAsync(ct);

        return outbox.Id;
    }

    public async Task<List<MessageResponse>> GetOutboxAsync(int employeeId, CancellationToken ct)
    {
        return await _db.Outbox.AsNoTracking()
            .Where(x => x.EmployeeFromId == employeeId)
            .OrderByDescending(x => x.CreateDate)
            .Select(x => new MessageResponse
            {
                Id = x.Id,
                FlowDesignsId = x.FlowDesignsId,
                FlowNodesId = x.FlowNodesId,
                EmployeeToId = x.EmployeeToId,
                EmployeeFromId = x.EmployeeFromId,
                EmailTo = x.EmailTo,
                EmailFrom = x.EmailFrom,
                Subject = x.Subject,
                CreateDate = x.CreateDate,
                UpdateDate = x.UpdateDate
            })
            .ToListAsync(ct);
    }

    public async Task<List<MessageResponse>> GetInboxAsync(int employeeId, CancellationToken ct)
    {
        return await _db.Inbox.AsNoTracking()
            .Where(x => x.EmployeeToId == employeeId)
            .OrderByDescending(x => x.CreateDate)
            .Select(x => new MessageResponse
            {
                Id = x.Id,
                FlowDesignsId = x.FlowDesignsId,
                FlowNodesId = x.FlowNodesId,
                EmployeeToId = x.EmployeeToId,
                EmployeeFromId = x.EmployeeFromId,
                EmailTo = x.EmailTo,
                EmailFrom = x.EmailFrom,
                Subject = x.Subject,
                CreateDate = x.CreateDate,
                UpdateDate = x.UpdateDate
            })
            .ToListAsync(ct);
    }

    public async Task<MarkAsReadResponse?> MarkInboxAsReadAsync(int messageId, CancellationToken ct)
    {
        var msg = await _db.Inbox.FirstOrDefaultAsync(x => x.Id == messageId, ct);
        if (msg == null) return null;

        if (msg.UpdateDate == null)
        {
            msg.UpdateDate = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return new MarkAsReadResponse
        {
            Id = msg.Id,
            UpdateDate = msg.UpdateDate!.Value
        };
    }
}
