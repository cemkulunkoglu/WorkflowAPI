using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Workflow.MessagesService.Entities;

namespace Workflow.MessagesService.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        using var mailMessage = new MailMessage(_options.From, message.EmailTo)
        {
            Subject = message.Subject,
            Body = message.Subject
        };

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = false
        };

        if (!string.IsNullOrWhiteSpace(_options.User))
        {
            client.Credentials = new NetworkCredential(_options.User, _options.Pass);
        }

        _logger.LogInformation("Sending email for outbox {OutboxId} to {EmailTo}", message.Id, message.EmailTo);
        await client.SendMailAsync(mailMessage, cancellationToken);
    }
}
