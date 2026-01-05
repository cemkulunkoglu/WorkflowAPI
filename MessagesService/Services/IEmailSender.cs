namespace MessagesService.Services;

public interface IEmailSender
{
    Task SendAsync(string from, string to, string subject, string body, CancellationToken ct);
}
