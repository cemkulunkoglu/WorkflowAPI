using Microsoft.AspNetCore.Mvc;
using Workflow.MessagesService.DTOs;
using Workflow.MessagesService.Services;

namespace Workflow.MessagesService.Controllers;

[ApiController]
[Route("api/messages")]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessagesController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendAsync([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var id = await _messageService.CreateOutboxAsync(request, cancellationToken);
        return Created("/api/messages/outbox", new { id });
    }

    [HttpGet("outbox")]
    public async Task<IActionResult> GetOutboxAsync([FromQuery] int employeeId, CancellationToken cancellationToken)
    {
        var messages = await _messageService.GetOutboxAsync(employeeId, cancellationToken);
        return Ok(messages);
    }

    [HttpGet("inbox")]
    public async Task<IActionResult> GetInboxAsync([FromQuery] int employeeId, CancellationToken cancellationToken)
    {
        var messages = await _messageService.GetInboxAsync(employeeId, cancellationToken);
        return Ok(messages);
    }
}
