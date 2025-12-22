using System.Collections.Concurrent;

namespace Workflow.ChatService.Streaming
{
    public class SseBroker
    {
        private readonly ConcurrentDictionary<Guid, HttpResponse> _clients = new();

        public async Task SubscribeAsync(HttpContext context)
        {
            var response = context.Response;

            response.Headers.Add("Content-Type", "text/event-stream");
            response.Headers.Add("Cache-Control", "no-cache");
            response.Headers.Add("Connection", "keep-alive");

            await response.Body.FlushAsync();

            var clientId = Guid.NewGuid();
            _clients.TryAdd(clientId, response);

            try
            {
                await Task.Delay(Timeout.Infinite, context.RequestAborted);
            }
            catch (TaskCanceledException)
            {
                // client disconnect
            }
            finally
            {
                _clients.TryRemove(clientId, out _);
            }
        }

        public void Publish(string json)
        {
            foreach (var client in _clients.Values)
            {
                try
                {
                    _ = client.WriteAsync($"event: message\ndata: {json}\n\n");
                    _ = client.Body.FlushAsync();
                }
                catch
                {
                    // ignore broken connections
                }
            }
        }
    }
}
