using MessagesService.Data;
using MessagesService.Entities;
using MessagesService.Events;
using MessagesService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MessagesService.Workers;

public class LeaveRequestCreatedHandler : ILeaveRequestCreatedHandler
{
    private readonly IConfiguration _config;
    private readonly IApproverEmailLookup _emailLookup;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LeaveRequestCreatedHandler> _logger;

    public LeaveRequestCreatedHandler(
        IConfiguration config,
        IApproverEmailLookup emailLookup,
        IServiceScopeFactory scopeFactory,
        ILogger<LeaveRequestCreatedHandler> logger)
    {
        _config = config;
        _emailLookup = emailLookup;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(LeaveRequestCreatedEvent payload, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MessagesDbContext>();

        // ✅ Config: kaç gün ve üzeri 2 üst mail alsın?
        var secondApproverMinDays = int.TryParse(_config["LeaveRequest:SecondApproverMinDays"], out var d) ? d : 4;

        // ✅ Employee.Path üzerinden alıcıları çıkar
        var path = await _emailLookup.GetEmployeePathByEmployeeIdAsync(payload.EmployeeId, ct);

        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogWarning("[LeaveRequestCreated] Path not found. EmployeeId={EmployeeId}", payload.EmployeeId);
            return false;
        }

        var pathIds = ParsePathIds(path);
        if (pathIds.Count < 2)
        {
            _logger.LogWarning(
                "[LeaveRequestCreated] Path does not include manager. EmployeeId={EmployeeId} Path={Path}",
                payload.EmployeeId, path);

            return false;
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

            return false;
        }

        _logger.LogInformation(
            "[LeaveRequestCreated] Recipients resolved. LeaveRequestId={LeaveRequestId} EmployeeId={EmployeeId} DayCount={DayCount} Recipients={Recipients}",
            payload.LeaveRequestId, payload.EmployeeId, payload.DayCount, string.Join(",", recipients));

        // ✅ Template hazırlığı (bir kere)
        var templateName = _config["LeaveRequest:TemplateName"] ?? "LEAVE_REQUEST_CREATED";

        var template = await db.MessageTemplates
            .FirstOrDefaultAsync(t => t.Name == templateName && t.IsActive, ct);

        if (template != null)
            _logger.LogInformation("[LeaveRequestCreated] Template found. Name={TemplateName}", templateName);
        else
            _logger.LogWarning("[LeaveRequestCreated] Template NOT found. Using fallback. Name={TemplateName}", templateName);

        // isimler (employee)
        var employeeName = await _emailLookup
            .GetEmployeeFullNameByEmployeeIdAsync(payload.EmployeeId, ct)
            ?? $"Employee#{payload.EmployeeId}";

        // URL'ler
        var baseUrl = _config["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var inboxUrl = $"{baseUrl.TrimEnd('/')}/dashboard?tab=messages&box=inbox";

        // UI template Onayla / Reddet kullanıyorsa diye:
        var approveUrl = inboxUrl;
        var rejectUrl = inboxUrl;

        var from = _config["Smtp:From"] ?? "noreply@workflow.local";

        var anyInserted = false;

        foreach (var toEmployeeId in recipients)
        {
            // ✅ Idempotency per recipient
            var messageId = $"{payload.LeaveRequestId}:{toEmployeeId}";

            var alreadyProcessed = await db.ProcessedMessages
                .AnyAsync(x => x.MessageId == messageId, ct);

            if (alreadyProcessed)
            {
                _logger.LogInformation(
                    "[LeaveRequestCreated] Already processed for recipient. MessageId={MessageId}",
                    messageId);

                continue;
            }

            // email + approver name
            var toEmail = await _emailLookup.GetEmployeeEmailByEmployeeIdAsync(toEmployeeId, ct);
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning(
                    "[LeaveRequestCreated] Recipient email not found. ToEmployeeId={ToEmployeeId}",
                    toEmployeeId);

                continue;
            }

            var approverName = await _emailLookup
                .GetEmployeeFullNameByEmployeeIdAsync(toEmployeeId, ct)
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
            await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[LeaveRequestCreated] Completed. LeaveRequestId={LeaveRequestId} Inserted={InsertedCount}",
            payload.LeaveRequestId, anyInserted ? "YES" : "NO (already processed / missing emails)");

        return anyInserted;
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
