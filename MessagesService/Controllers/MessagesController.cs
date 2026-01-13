using MessagesService.Dtos;
using MessagesService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace MessagesService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _service;
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;

    public MessagesController(IMessageService service, ILogger<MessagesController> logger, IConfiguration configuration)
    {
        _service = service;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var employeeFromIdStr = User.FindFirstValue("employeeId")
                              ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(employeeFromIdStr, out var employeeFromId))
            return Unauthorized("Token içinde employeeId bulunamadı.");

        var emailFrom = _configuration["Smtp:From"]
                ?? User.FindFirst(ClaimTypes.Email)?.Value;

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
        var employeeIdStr = User.FindFirstValue("employeeId")
                          ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(employeeIdStr, out var employeeId))
            return Unauthorized("Token içinde employeeId bulunamadı.");

        var result = await _service.MarkInboxAsReadAsync(id, employeeId, ct);
        if (result == null) return NotFound();

        return Ok(result);
    }


    [HttpGet("inbox/{id:int}")]
    public async Task<IActionResult> GetInboxById([FromRoute] int id, CancellationToken ct)
    {
        var employeeIdStr = User.FindFirstValue("employeeId")
                          ?? User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (!int.TryParse(employeeIdStr, out var employeeId))
            return Unauthorized("Token içinde employeeId bulunamadı.");

        var result = await _service.GetInboxByIdAsync(id, employeeId, ct);
        if (result == null) return NotFound();

        return Ok(result);
    }


}
