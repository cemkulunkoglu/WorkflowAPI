using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MessagesService.Dtos;
using MessagesService.Services;

namespace MessagesService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _service;

    public MessagesController(IMessageService service)
    {
        _service = service;
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var employeeFromIdStr = User.FindFirstValue("employeeId")
                              ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(employeeFromIdStr, out var employeeFromId))
            return Unauthorized("Token içinde employeeId bulunamadı.");

        var emailFrom = User.FindFirstValue("email") ?? User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(emailFrom))
            return Unauthorized("Token içinde email bulunamadı.");

        var id = await _service.SendAsync(request, employeeFromId, emailFrom, ct);
        return CreatedAtAction(nameof(GetOutbox), new { employeeId = employeeFromId }, new { id });
    }

    [HttpGet("outbox")]
    public async Task<IActionResult> GetOutbox([FromQuery] int employeeId, CancellationToken ct)
        => Ok(await _service.GetOutboxAsync(employeeId, ct));


    [HttpGet("inbox")]
    public async Task<IActionResult> GetInbox([FromQuery] int employeeId, CancellationToken ct)
        => Ok(await _service.GetInboxAsync(employeeId, ct));


    [HttpPut("inbox/{id:int}/read")]
    public async Task<IActionResult> MarkInboxAsRead([FromRoute] int id, CancellationToken ct)
    {
        var result = await _service.MarkInboxAsReadAsync(id, ct);
        if (result == null) return NotFound();

        return Ok(result);
    }
}
