namespace Workflow.ChatService.Services
{
    public class MockAiService : IAiService
    {
        public Task<string> ReplyAsync(string userText, CancellationToken ct)
        {
            // Şimdilik basit mock: sonradan OpenAI/LLM bağlarız
            var reply = $"AI: \"{userText}\" mesajını aldım ✅";
            return Task.FromResult(reply);
        }
    }
}
