using MessagesService.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MessagesService.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;

    public SmtpEmailSender(Microsoft.Extensions.Options.IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(string from, string to, string subject, string body, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        message.Body = new TextPart("html") { Text = body };

        using var client = new SmtpClient();

        client.CheckCertificateRevocation = false;

        var options = _options.Port switch
        {
            465 => SecureSocketOptions.SslOnConnect,
            587 => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.Auto
        };

        await client.ConnectAsync(_options.Host, _options.Port, options, ct);

        if (!string.IsNullOrWhiteSpace(_options.User))
            await client.AuthenticateAsync(_options.User, _options.Password, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
