using Workflow.MessagesService.DTOs;
using Workflow.MessagesService.Entities;

namespace Workflow.MessagesService.Services;

public interface IMessageService
{
    Task<int> CreateOutboxAsync(SendMessageRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<OutboxMessage>> GetOutboxAsync(int employeeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<InboxMessage>> GetInboxAsync(int employeeId, CancellationToken cancellationToken);
}
