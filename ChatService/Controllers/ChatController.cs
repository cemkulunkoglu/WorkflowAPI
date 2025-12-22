using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Workflow.ChatService.DTOs;
using Workflow.ChatService.Events;
using Workflow.ChatService.Messaging;

namespace Workflow.ChatService.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly RabbitMqProducer _producer;

        public ChatController(RabbitMqProducer producer)
        {
            _producer = producer;
        }

        [Authorize]
        [HttpPost("send")]
        public ActionResult<ChatMessageCreatedEvent> Send([FromBody] SendMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("Text zorunlu.");

            if (request.SenderType != "User" && request.SenderType != "AI")
                return BadRequest("SenderType sadece 'User' veya 'AI' olabilir.");

            // ✅ ClientId artık JWT’den
            var clientId =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? User.FindFirstValue("userId")
                ?? "unknown-user";

            var evt = new ChatMessageCreatedEvent
            {
                ThreadId = request.ThreadId ?? "global",
                ClientId = clientId,

                MessageId = Guid.NewGuid().ToString("N"),
                SenderType = request.SenderType,
                Text = request.Text,
                CreatedAtUtc = DateTime.UtcNow
            };

            _producer.PublishMessageCreated(evt);
            return Ok(evt);
        }
    }
}
