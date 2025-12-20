using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Workflow.ChatService.Domain;
using Workflow.ChatService.Events;
using Workflow.ChatService.Hubs;
using Workflow.ChatService.Storage;

namespace Workflow.ChatService.Messaging
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly IConfiguration _config;
        private readonly IChatStore _store;
        private readonly IHubContext<ChatHub> _hub;

        private IConnection? _connection;
        private IModel? _channel;

        private static readonly ConcurrentDictionary<string, byte> ProcessedMessageIds = new();

        public RabbitMqConsumer(IConfiguration config, IChatStore store, IHubContext<ChatHub> hub)
        {
            _config = config;
            _store = store;
            _hub = hub;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var section = _config.GetSection("RabbitMQ");

            var factory = new ConnectionFactory
            {
                HostName = section["HostName"],
                UserName = section["UserName"],
                Password = section["Password"],
                Port = int.Parse(section["Port"] ?? "5672")
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            var exchange = section["Exchange"]!;
            var queue = section["Queue"]!;
            var routingKey = section["RoutingKey"]!;

            _channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Topic, durable: true);
            _channel.QueueDeclare(queue: queue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue: queue, exchange: exchange, routingKey: routingKey);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel is null)
                throw new InvalidOperationException("RabbitMQ channel is not initialized.");

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (sender, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var evt = JsonSerializer.Deserialize<ChatMessageCreatedEvent>(json);

                    if (evt is null || string.IsNullOrWhiteSpace(evt.ThreadId))
                    {
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(evt.MessageId)
                        && !ProcessedMessageIds.TryAdd(evt.MessageId, 0))
                    {
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                        return;
                    }

                    var msg = new ChatMessage
                    {
                        MessageId = string.IsNullOrWhiteSpace(evt.MessageId) ? Guid.NewGuid().ToString() : evt.MessageId,
                        SenderType = evt.SenderType,
                        Text = evt.Text,
                        CreatedAt = evt.CreatedAtUtc
                    };

                    _store.AddMessage(evt.ThreadId, msg);

                    await _hub.Clients.Group(evt.ThreadId).SendAsync("message:new", msg, stoppingToken);

                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch
                {
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(queue: _config["RabbitMQ:Queue"]!, autoAck: false, consumer: consumer);

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
