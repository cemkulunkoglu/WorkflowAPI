using Microsoft.AspNetCore.Mvc;
using Workflow.ChatService.Domain;
using Workflow.ChatService.DTOs;
using Workflow.ChatService.Events;
using Workflow.ChatService.Messaging;
using Workflow.ChatService.Storage;

namespace Workflow.ChatService.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IChatStore _store;
        private readonly RabbitMqProducer _producer;

        public ChatController(IChatStore store, RabbitMqProducer producer)
        {
            _store = store;
            _producer = producer;
        }

        // POST: api/chat/threads/get-or-create
        [HttpPost("threads/get-or-create")]
        public ActionResult<GetOrCreateThreadResponse> GetOrCreateThread([FromBody] GetOrCreateThreadRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ClientId))
                return BadRequest("ClientId zorunlu.");

            if (request.Type != "AI" && request.Type != "HUMAN")
                return BadRequest("Type sadece 'AI' veya 'HUMAN' olabilir.");

            var thread = _store.GetOrCreateThread(
                request.ClientId,
                request.Type,
                request.ContextType,
                request.ContextId,
                request.ContextTitle
            );

            return Ok(new GetOrCreateThreadResponse { ThreadId = thread.ThreadId });
        }

        // GET: api/chat/threads/{threadId}/messages
        [HttpGet("threads/{threadId}/messages")]
        public ActionResult GetMessages([FromRoute] string threadId)
        {
            if (string.IsNullOrWhiteSpace(threadId))
                return BadRequest("threadId zorunlu.");

            var messages = _store.GetMessages(threadId);
            return Ok(messages);
        }

        // POST: api/chat/threads/{threadId}/messages
        [HttpPost("threads/{threadId}/messages")]
        public ActionResult SendMessage([FromRoute] string threadId, [FromBody] SendMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(threadId))
                return BadRequest("threadId zorunlu.");
            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("Text zorunlu.");

            var evt = new ChatMessageCreatedEvent
            {
                ThreadId = threadId,
                ClientId = "demo-client-1", // şimdilik. sonra JWT’den alırız.
                MessageId = Guid.NewGuid().ToString(),
                SenderType = request.SenderType,
                Text = request.Text,
                CreatedAtUtc = DateTime.UtcNow
            };

            _producer.PublishMessageCreated(evt);

            // Event-driven: kabul ettik, mesajı SignalR’dan göreceksin
            return Accepted(new { evt.MessageId, evt.ThreadId });
        }
    }
}
