using AuthServerAPI.Interfaces;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace AuthServerAPI.Messaging
{
    public class RabbitMqEventPublisher : IEventPublisher
    {
        private readonly IConnection _connection;
        private readonly string _exchange;

        public RabbitMqEventPublisher(IConfiguration config)
        {
            var factory = new ConnectionFactory
            {
                HostName = config["RabbitMQ:HostName"],
                UserName = config["RabbitMQ:UserName"],
                Password = config["RabbitMQ:Password"],
                Port = int.Parse(config["RabbitMQ:Port"] ?? "5672")
            };

            _exchange = config["RabbitMQ:Exchange"] ?? "workflow.events";
            _connection = factory.CreateConnection();
        }

        public void Publish<T>(string eventName, T payload)
        {
            using var channel = _connection.CreateModel();

            channel.ExchangeDeclare(
                exchange: _exchange,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false
            );

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                eventName,
                payload
            }));

            channel.BasicPublish(
                exchange: _exchange,
                routingKey: "",
                basicProperties: null,
                body: body
            );
        }
    }
}
