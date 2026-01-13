using MessagesService.Data;
using MessagesService.Dtos;
using MessagesService.Entities;
using MessagesService.Interfaces;
using Microsoft.EntityFrameworkCore;

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
        string emailSubject;
        string emailBody;
        string uiBody;

        if (request.TemplateId.HasValue || !string.IsNullOrWhiteSpace(request.TemplateName))
        {
            var template = await _db.MessageTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    request.TemplateId.HasValue
                        ? t.TemplateId == request.TemplateId.Value
                        : t.Name == request.TemplateName,
                    ct);

            if (template == null)
                throw new Exception("Message template bulunamadı.");

            // Email (SMTP)
            emailSubject = RenderTemplate(template.Subject ?? string.Empty, request.Fields);
            emailBody = RenderTemplate(template.Body ?? string.Empty, request.Fields);

            // UI (Inbox detail)
            // UiBody boşsa Body'ye fallback (minimum sürtünme)
            uiBody = RenderTemplate(template.UiBody ?? template.Body ?? string.Empty, request.Fields);
        }
        else
        {
            // Template yoksa: sadece subject, body boş
            emailSubject = request.Subject ?? string.Empty;
            emailBody = string.Empty;
            uiBody = string.Empty;
        }

        var outbox = new OutboxMessage
        {
            FlowDesignsId = request.FlowDesignsId,
            FlowNodesId = request.FlowNodesId,
            EmployeeToId = request.EmployeeToId,
            EmployeeFromId = employeeFromId,
            EmailTo = request.EmailTo,
            EmailFrom = emailFrom,

            Subject = emailSubject,
            Body = emailBody,

            UiBody = uiBody,

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
                UpdateDate = x.UpdateDate,

                // Read receipt (sender side)
                IsReadByReceiver = x.IsReadByReceiver,
                ReadByReceiverAt = x.ReadByReceiverAt
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
                UpdateDate = x.UpdateDate,

                // Inbox read state (receiver side)
                OutboxId = x.OutboxId,
                IsRead = x.IsRead,
                ReadAt = x.ReadAt
            })
            .ToListAsync(ct);
    }

    public async Task<InboxMessage?> MarkInboxAsReadAsync(int inboxId, int employeeId, CancellationToken ct)
    {
        var inbox = await _db.Inbox
            .FirstOrDefaultAsync(x => x.Id == inboxId && x.EmployeeToId == employeeId, ct);

        if (inbox == null) return null;

        if (!inbox.IsRead)
        {
            var now = DateTime.UtcNow;

            // 1) Inbox update
            inbox.IsRead = true;
            inbox.ReadAt = now;
            inbox.UpdateDate = now;

            // 2) Outbox update (sender sees "read")
            if (inbox.OutboxId.HasValue)
            {
                var outbox = await _db.Outbox.FirstOrDefaultAsync(x => x.Id == inbox.OutboxId.Value, ct);
                if (outbox != null && !outbox.IsReadByReceiver)
                {
                    outbox.IsReadByReceiver = true;
                    outbox.ReadByReceiverAt = now;
                    outbox.UpdateDate = now;
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        // Return updated inbox (entity)
        return inbox;
    }

    public async Task<MessageResponse?> GetInboxByIdAsync(int inboxId, int employeeId, CancellationToken ct)
    {
        var inbox = await _db.Inbox.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == inboxId && x.EmployeeToId == employeeId, ct);

        if (inbox == null) return null;

        string? uiBody = null;

        if (inbox.OutboxId.HasValue)
        {
            uiBody = await _db.Outbox.AsNoTracking()
                .Where(o => o.Id == inbox.OutboxId.Value)
                .Select(o => o.UiBody)
                .FirstOrDefaultAsync(ct);
        }

        return new MessageResponse
        {
            Id = inbox.Id,
            FlowDesignsId = inbox.FlowDesignsId,
            FlowNodesId = inbox.FlowNodesId,
            EmployeeToId = inbox.EmployeeToId,
            EmployeeFromId = inbox.EmployeeFromId,
            EmailTo = inbox.EmailTo,
            EmailFrom = inbox.EmailFrom,
            Subject = inbox.Subject,
            CreateDate = inbox.CreateDate,
            UpdateDate = inbox.UpdateDate,

            OutboxId = inbox.OutboxId,
            IsRead = inbox.IsRead,
            ReadAt = inbox.ReadAt,

            UiBody = uiBody
        };
    }

    private static string RenderTemplate(string template, Dictionary<string, string>? fields)
    {
        if (string.IsNullOrEmpty(template) || fields == null || fields.Count == 0)
            return template;

        foreach (var kv in fields)
        {
            template = template.Replace("{" + kv.Key + "}", kv.Value ?? string.Empty);
        }

        return template;
    }
}
