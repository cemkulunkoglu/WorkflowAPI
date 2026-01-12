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
        // 1) Subject/Body üret (template varsa template’ten, yoksa request’ten)
        string subject;
        string body;

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

            subject = RenderTemplate(template.Subject ?? string.Empty, request.Fields);
            body = RenderTemplate(template.Body ?? string.Empty, request.Fields);
        }
        else
        {
            subject = request.Subject;
            body = string.Empty;
        }

        // 2) Outbox kaydı oluştur
        var outbox = new OutboxMessage
        {
            FlowDesignsId = request.FlowDesignsId,
            FlowNodesId = request.FlowNodesId,
            EmployeeToId = request.EmployeeToId,
            EmployeeFromId = employeeFromId,
            EmailTo = request.EmailTo,
            EmailFrom = emailFrom,
            Subject = subject,
            Body = body,
            CreateDate = DateTime.UtcNow,
            UpdateDate = null
        };

        // 3) OutboxMessage entity’sinde Body alanı varsa set et (yoksa compile kırmadan geç)
        TrySetBodyIfExists(outbox, body);

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

    private static void TrySetBodyIfExists(OutboxMessage outbox, string body)
    {
        // OutboxMessage’da Body property’si yoksa hiçbir şey yapma
        var prop = typeof(OutboxMessage).GetProperty("Body");
        if (prop == null) return;
        if (prop.PropertyType != typeof(string)) return;

        prop.SetValue(outbox, body ?? string.Empty);
    }
}
