namespace Workflow.MessagesService.Services;

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string User { get; set; } = string.Empty;
    public string Pass { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
}
