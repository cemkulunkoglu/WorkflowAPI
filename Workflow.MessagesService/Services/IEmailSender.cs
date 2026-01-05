using Workflow.MessagesService.Entities;

namespace Workflow.MessagesService.Services;

public interface IEmailSender
{
    Task SendAsync(OutboxMessage message, CancellationToken cancellationToken);
}
