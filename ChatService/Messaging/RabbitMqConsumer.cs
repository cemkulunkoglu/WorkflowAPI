using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Workflow.ChatService.Events;
using Workflow.ChatService.Messaging;
using Workflow.ChatService.Services;
using Workflow.ChatService.Streaming;

namespace Workflow.ChatService.Messaging
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly IConfiguration _config;
        private readonly SseBroker _broker;
        private readonly RabbitMqProducer _producer;
        private readonly IAiService _ai;

        private IConnection? _connection;
        private IModel? _channel;
        private string? _queueName;
        private string? _exchange;

        // duplicate koruması (consumer restart olunca sıfırlanır; şimdilik yeterli)
        private readonly HashSet<string> _seenMessageIds = new();

        public RabbitMqConsumer(
            IConfiguration config,
            SseBroker broker,
            RabbitMqProducer producer,
            IAiService ai)
        {
            _config = config;
            _broker = broker;
            _producer = producer;
            _ai = ai;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var section = _config.GetSection("RabbitMQ");

            var host = section["HostName"];
            var user = section["UserName"];
            var pass = section["Password"];
            var port = int.Parse(section["Port"] ?? "5672");
            _exchange = section["Exchange"];

            if (string.IsNullOrWhiteSpace(_exchange))
                throw new InvalidOperationException("RabbitMQ:Exchange ayarı zorunlu.");

            var factory = new ConnectionFactory
            {
                HostName = host,
                UserName = user,
                Password = pass,
                Port = port,
                DispatchConsumersAsync = true // ✅ async consumer için şart
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(exchange: _exchange, type: ExchangeType.Fanout, durable: true);

            _queueName = _channel.QueueDeclare(
                queue: "",
                durable: false,
                exclusive: true,
                autoDelete: true
            ).QueueName;

            _channel.QueueBind(queue: _queueName, exchange: _exchange, routingKey: "");

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 50, global: false);

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel is null || string.IsNullOrWhiteSpace(_queueName))
                throw new InvalidOperationException("RabbitMQ not initialized.");

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (_, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                    // ✅ 1) SSE’ye yayınla (ham json)
                    _broker.Publish(json);

                    // ✅ 2) JSON → event parse (AI tetiklemek için)
                    ChatMessageCreatedEvent? evt = null;
                    try
                    {
                        evt = JsonSerializer.Deserialize<ChatMessageCreatedEvent>(json,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch
                    {
                        // parse edilemezse sadece SSE’ye basıp geçiyoruz
                    }

                    // ✅ 3) Duplicate kontrol + AI tetikleme
                    if (evt is not null && !string.IsNullOrWhiteSpace(evt.MessageId))
                    {
                        lock (_seenMessageIds)
                        {
                            if (_seenMessageIds.Contains(evt.MessageId))
                            {
                                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                                return;
                            }
                            _seenMessageIds.Add(evt.MessageId);
                        }

                        // Sadece User mesajına AI cevap üret (AI mesajı gelince döngü olmasın)
                        if (string.Equals(evt.SenderType, "User", StringComparison.OrdinalIgnoreCase))
                        {
                            var replyText = await _ai.ReplyAsync(evt.Text ?? "", stoppingToken);

                            var aiEvt = new ChatMessageCreatedEvent
                            {
                                ThreadId = evt.ThreadId ?? "global",
                                ClientId = "ai-bot",
                                MessageId = Guid.NewGuid().ToString("N"),
                                SenderType = "AI",
                                Text = replyText,
                                CreatedAtUtc = DateTime.UtcNow,
                                ReplyToMessageId = evt.MessageId
                            };

                            _producer.PublishMessageCreated(aiEvt);
                        }
                    }

                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch
                {
                    _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
            return Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override void Dispose()
        {
            try { _channel?.Close(); } catch { }
            try { _connection?.Close(); } catch { }
            base.Dispose();
        }
    }
}
