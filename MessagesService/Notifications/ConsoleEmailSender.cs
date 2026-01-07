using MessagesService.Services;

namespace MessagesService.Notifications;

public class ConsoleEmailSender : IEmailSender
{
    public Task SendAsync(string from, string to, string subject, string body, CancellationToken ct)
    {
        Console.WriteLine("=== EMAIL (SIMULATED) ===");
        Console.WriteLine($"FROM: {from}");
        Console.WriteLine($"TO: {to}");
        Console.WriteLine($"SUBJECT: {subject}");
        Console.WriteLine(body);
        Console.WriteLine("=========================");
        return Task.CompletedTask;
    }
}
