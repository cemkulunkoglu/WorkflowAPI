using MessagesService.Dtos;
using Workflow.MessagesService.Dtos;

namespace MessagesService.Services;

public interface IMessageService
{
    Task<int> SendAsync(SendMessageRequest request, int employeeFromId, string emailFrom, CancellationToken ct);
    Task<List<MessageResponse>> GetOutboxAsync(int employeeId, CancellationToken ct);
    Task<List<MessageResponse>> GetInboxAsync(int employeeId, CancellationToken ct);
}
