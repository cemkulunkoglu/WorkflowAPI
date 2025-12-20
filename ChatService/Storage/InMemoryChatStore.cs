using System.Collections.Concurrent;
using Workflow.ChatService.Domain;

namespace Workflow.ChatService.Storage
{
    public class InMemoryChatStore : IChatStore
    {
        private readonly ConcurrentDictionary<string, ChatThread> _threads = new();

        public ChatThread GetOrCreateThread(
            string clientId,
            string type,
            string? contextType,
            string? contextId,
            string? contextTitle)
        {
            var existing = _threads.Values.FirstOrDefault(t =>
                t.ClientId == clientId &&
                t.Type == type &&
                t.ContextType == contextType &&
                t.ContextId == contextId
            );

            if (existing != null)
                return existing;

            var thread = new ChatThread
            {
                ClientId = clientId,
                Type = type,
                ContextType = contextType,
                ContextId = contextId,
                ContextTitle = contextTitle
            };

            _threads[thread.ThreadId] = thread;
            return thread;
        }

        public IReadOnlyList<ChatMessage> GetMessages(string threadId)
        {
            return _threads.TryGetValue(threadId, out var thread)
                ? thread.Messages
                : Array.Empty<ChatMessage>();
        }

        public void AddMessage(string threadId, ChatMessage message)
        {
            if (!_threads.TryGetValue(threadId, out var thread))
            {
                thread = new ChatThread
                {
                    ThreadId = threadId,
                    ClientId = "unknown",
                    Type = "AI"
                };
                _threads[threadId] = thread;
            }

            thread.Messages.Add(message);

            // RAM koruması (maks 200 mesaj)
            if (thread.Messages.Count > 200)
            {
                thread.Messages.RemoveAt(0);
            }
        }
    }
}
