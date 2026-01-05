using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Workflow.MessagesService.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _opt;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> opt, ILogger<SmtpEmailSender> logger)
    {
        _opt = opt.Value;
        _logger = logger;
    }

    public async Task SendAsync(string from, string to, string subject, string body, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        message.Body = new TextPart("plain")
        {
            Text = body
        };

        using var client = new SmtpClient();

        // Mailpit self-signed vs yok; SSL kapalı
        await client.ConnectAsync(_opt.Host, _opt.Port, _opt.UseSsl, ct);

        if (!string.IsNullOrWhiteSpace(_opt.User))
        {
            await client.AuthenticateAsync(_opt.User, _opt.Password, ct);
        }

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        _logger.LogInformation("Mail sent. From={From} To={To} Subject={Subject}", from, to, subject);
    }
}
