using MessagesService.Dtos;
using Microsoft.EntityFrameworkCore;
using Workflow.MessagesService.Data;
using Workflow.MessagesService.Dtos;
using Workflow.MessagesService.Entities;

namespace MessagesService.Services;

public class MessageService : IMessageService
{
    private readonly MessagesDbContext _db;

    public MessageService(MessagesDbContext db)
    {
        _db = db;
    }

    public async Task<int> SendAsync(SendMessageRequest request, int employeeFromId, string emailFrom, CancellationToken ct)
    {
        var outbox = new OutboxMessage
        {
            FlowDesignsId = request.FlowDesignsId,
            FlowNodesId = request.FlowNodesId,
            EmployeeToId = request.EmployeeToId,
            EmployeeFromId = employeeFromId,   // ✅ token’dan
            EmailTo = request.EmailTo,
            EmailFrom = emailFrom,             // ✅ token’dan
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
}
