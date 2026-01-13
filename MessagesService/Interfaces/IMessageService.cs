using MessagesService.Dtos;
using MessagesService.Entities;

namespace MessagesService.Interfaces;

public interface IMessageService
{
    Task<int> SendAsync(SendMessageRequest request, int employeeFromId, string emailFrom, CancellationToken ct);
    Task<List<MessageResponse>> GetOutboxAsync(int employeeId, CancellationToken ct);
    Task<List<MessageResponse>> GetInboxAsync(int employeeId, CancellationToken ct);
    Task<InboxMessage?> MarkInboxAsReadAsync(int inboxId, int employeeId, CancellationToken ct);
    Task<MessageResponse?> GetInboxByIdAsync(int inboxId, int employeeId, CancellationToken ct);



}
