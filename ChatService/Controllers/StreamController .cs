using Microsoft.AspNetCore.Mvc;
using Workflow.ChatService.Streaming;

namespace Workflow.ChatService.Controllers
{
    [ApiController]
    public class StreamController : ControllerBase
    {
        private readonly SseBroker _broker;

        public StreamController(SseBroker broker)
        {
            _broker = broker;
        }

        [HttpGet("/stream")]
        public async Task Stream(CancellationToken ct)
        {
            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";
            Response.Headers["X-Accel-Buffering"] = "no";

            var (clientId, reader) = _broker.Subscribe();

            try
            {
                await Response.WriteAsync("event: ping\ndata: connected\n\n", ct);
                await Response.Body.FlushAsync(ct);

                while (await reader.WaitToReadAsync(ct))
                {
                    while (reader.TryRead(out var payload))
                    {
                        await Response.WriteAsync($"event: message\ndata: {payload}\n\n", ct);
                        await Response.Body.FlushAsync(ct);
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                _broker.Unsubscribe(clientId);
            }
        }
    }
}
