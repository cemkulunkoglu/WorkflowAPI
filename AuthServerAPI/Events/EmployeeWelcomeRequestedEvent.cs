namespace AuthServerAPI.Events;

public class EmployeeWelcomeRequestedEvent
{
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public int UserId { get; set; }
    public int EmployeeId { get; set; }
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string TemporaryPassword { get; set; } = null!;
}
