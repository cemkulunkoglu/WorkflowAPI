namespace Workflow.MessagesService.Services;

public class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;

    public string? User { get; set; }
    public string? Password { get; set; }

    public bool UseSsl { get; set; } = false;
}
