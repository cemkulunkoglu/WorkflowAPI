using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Workflow.ChatService.Domain;
using Workflow.ChatService.DTOs;
using Workflow.ChatService.Events;
using Workflow.ChatService.Hubs;
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
        private readonly IHubContext<ChatHub> _hub;

        public ChatController(
            IChatStore store,
            RabbitMqProducer producer,
            IHubContext<ChatHub> hub)
        {
            _store = store;
            _producer = producer;
            _hub = hub;
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
        // - RAM'e yazar
        // - SignalR ile gruba yayınlar
        // - İsteğe bağlı RabbitMQ event basar
        [HttpPost("threads/{threadId}/messages")]
        public async Task<ActionResult> SendMessage([FromRoute] string threadId, [FromBody] SendMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(threadId))
                return BadRequest("threadId zorunlu.");

            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("Text zorunlu.");

            if (request.SenderType != "User" && request.SenderType != "AI")
                return BadRequest("SenderType sadece 'User' veya 'AI' olabilir.");

            // 1) RAM'e yaz (DB yok)
            var msg = new ChatMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                SenderType = request.SenderType,
                Text = request.Text,
                CreatedAt = DateTime.UtcNow
            };

            _store.AddMessage(threadId, msg);

            // 2) SignalR broadcast (thread grubuna)
            await _hub.Clients.Group(threadId).SendAsync("message:new", msg);

            // 3) Opsiyonel: Event publish (ileride consumer ile scale için)
            // Not: ClientId şimdilik body’den veya sabitten geliyor; sonra JWT claim’den alacağız.
            var evt = new ChatMessageCreatedEvent
            {
                ThreadId = threadId,
                ClientId = "demo-client-1",
                MessageId = msg.MessageId,
                SenderType = msg.SenderType,
                Text = msg.Text,
                CreatedAtUtc = msg.CreatedAt
            };

            _producer.PublishMessageCreated(evt);

            // 4) Response: UI/WorkflowWeb hemen mesajı alabilsin
            return Ok(msg);
        }
    }
}
