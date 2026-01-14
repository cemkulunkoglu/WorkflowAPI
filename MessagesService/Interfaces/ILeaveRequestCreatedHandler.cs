using MessagesService.Events;

namespace MessagesService.Interfaces;

public interface ILeaveRequestCreatedHandler
{
    Task<bool> HandleAsync(LeaveRequestCreatedEvent payload, CancellationToken ct);
}
