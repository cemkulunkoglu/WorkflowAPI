using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Workflow.ChatService.Events;

namespace Workflow.ChatService.Messaging
{
    public class RabbitMqProducer : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        private readonly string _exchange;
        private readonly string _routingKey;

        public RabbitMqProducer(IConfiguration config)
        {
            var section = config.GetSection("RabbitMQ");
            _exchange = section["Exchange"]!;
            _routingKey = section["RoutingKey"]!;

            var factory = new ConnectionFactory
            {
                HostName = section["HostName"],
                UserName = section["UserName"],
                Password = section["Password"],
                Port = int.Parse(section["Port"] ?? "5672")
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(exchange: _exchange, type: ExchangeType.Topic, durable: true);
        }

        public void PublishMessageCreated(ChatMessageCreatedEvent evt)
        {
            var json = JsonSerializer.Serialize(evt);
            var body = Encoding.UTF8.GetBytes(json);

            var props = _channel.CreateBasicProperties();
            props.Persistent = false;
            props.MessageId = evt.MessageId;

            _channel.BasicPublish(
                exchange: _exchange,
                routingKey: _routingKey,
                basicProperties: props,
                body: body);
        }

        public void Dispose()
        {
            try { _channel.Close(); } catch { }
            try { _connection.Close(); } catch { }
        }
    }
}
