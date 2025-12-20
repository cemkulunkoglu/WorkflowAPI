using Microsoft.AspNetCore.SignalR;

namespace Workflow.ChatService.Hubs
{
    public class ChatHub : Hub
    {
        public Task JoinThread(string threadId)
            => Groups.AddToGroupAsync(Context.ConnectionId, threadId);

        public Task LeaveThread(string threadId)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, threadId);
    }
}
