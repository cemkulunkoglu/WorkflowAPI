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

        public RabbitMqProducer(IConfiguration config)
        {
            var section = config.GetSection("RabbitMQ");

            _exchange = section["Exchange"]
                ?? throw new InvalidOperationException("RabbitMQ:Exchange tanımlı değil.");

            var factory = new ConnectionFactory
            {
                HostName = section["HostName"],
                UserName = section["UserName"],
                Password = section["Password"],
                Port = int.Parse(section["Port"] ?? "5672")
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // 🔥 Fanout exchange → tüm consumer’lara yayın
            _channel.ExchangeDeclare(
                exchange: _exchange,
                type: ExchangeType.Fanout,
                durable: true
            );
        }

        // 🔹 Generic publish (istersen ileride başka event’ler için)
        public void Publish(object evt)
        {
            var json = JsonSerializer.Serialize(evt);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(
                exchange: _exchange,
                routingKey: "",       // fanout
                basicProperties: null,
                body: body
            );
        }

        // 🔹 ChatController’ın çağırdığı NET ve OKUNAKLI API
        public void PublishMessageCreated(ChatMessageCreatedEvent evt)
        {
            Publish(evt);
        }

        public void Dispose()
        {
            try { _channel?.Close(); } catch { }
            try { _connection?.Close(); } catch { }
        }
    }
}
