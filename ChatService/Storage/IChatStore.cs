using Workflow.ChatService.Domain;

namespace Workflow.ChatService.Storage
{
    public interface IChatStore
    {
        ChatThread GetOrCreateThread(
            string clientId,
            string type,
            string? contextType,
            string? contextId,
            string? contextTitle
        );

        IReadOnlyList<ChatMessage> GetMessages(string threadId);

        void AddMessage(string threadId, ChatMessage message);
    }
}
