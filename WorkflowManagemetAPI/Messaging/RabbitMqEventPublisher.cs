using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using WorkflowManagemetAPI.Interfaces.Messaging;

namespace WorkflowManagemetAPI.Messaging
{
    public class RabbitMqEventPublisher : IEventPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _exchangeName;

        public RabbitMqEventPublisher(IConfiguration config)
        {
            _exchangeName = config["RabbitMQ:Exchange"] ?? "workflow.events";

            var factory = new ConnectionFactory
            {
                HostName = config["RabbitMQ:HostName"] ?? "localhost",
                UserName = config["RabbitMQ:UserName"] ?? "guest",
                Password = config["RabbitMQ:Password"] ?? "guest",
                Port = int.TryParse(config["RabbitMQ:Port"], out var p) ? p : 5672,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Fanout, durable: true);
        }

        public void Publish(string eventName, object payload)
        {
            var envelope = new { eventName, payload };
            var json = JsonSerializer.Serialize(envelope);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(exchange: _exchangeName, routingKey: "", basicProperties: null, body: body);
        }

        public void Dispose()
        {
            try { _channel?.Close(); } catch { }
            try { _connection?.Close(); } catch { }
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
