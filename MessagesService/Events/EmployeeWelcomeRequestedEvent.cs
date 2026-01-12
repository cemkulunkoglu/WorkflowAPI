namespace MessagesService.Events;

public class EmployeeWelcomeRequestedEvent
{
    public Guid MessageId { get; set; }

    public int UserId { get; set; }
    public int EmployeeId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public string TemporaryPassword { get; set; } = string.Empty;
}
