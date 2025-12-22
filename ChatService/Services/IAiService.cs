namespace Workflow.ChatService.Services
{
    public interface IAiService
    {
        Task<string> ReplyAsync(string userText, CancellationToken ct);
    }
}
