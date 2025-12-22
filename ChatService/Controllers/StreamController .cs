using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Workflow.ChatService.Streaming;

[ApiController]
[Route("stream")]
public class StreamController : ControllerBase
{
    private readonly SseBroker _broker;

    public StreamController(SseBroker broker)
    {
        _broker = broker;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task Get()
    {
        await _broker.SubscribeAsync(HttpContext);
    }
}
