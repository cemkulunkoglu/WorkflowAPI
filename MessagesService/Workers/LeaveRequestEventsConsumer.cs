using MessagesService.Data;
using MessagesService.Entities;
using MessagesService.Events;
using MessagesService.Interfaces;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace MessagesService.Workers;

public class LeaveRequestEventsConsumer : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IApproverEmailLookup _emailLookup;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LeaveRequestEventsConsumer> _logger;
    private readonly ILeaveRequestCreatedHandler _handler;

    private RabbitMQ.Client.IConnection? _connection;
    private RabbitMQ.Client.IModel? _channel;
    private string _queueName = "";

    public LeaveRequestEventsConsumer(
        IConfiguration config,
        IApproverEmailLookup emailLookup,
        IServiceScopeFactory scopeFactory,
        ILogger<LeaveRequestEventsConsumer> logger,
        ILeaveRequestCreatedHandler handler)
    {
        _config = config;
        _emailLookup = emailLookup;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _handler = handler;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var exchange = _config["RabbitMQ:Exchange"] ?? "workflow.events";
        _queueName = _config["RabbitMQ:LeaveRequestsQueue"] ?? "workflow.messages.leave-requests";

        // DLQ/DLX settings
        var dlxExchange = "workflow.events.dlx";
        var dlRoutingKey = "leave-requests";
        var dlqName = _queueName + ".dlq";

        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMQ:HostName"] ?? "localhost",
            UserName = _config["RabbitMQ:UserName"] ?? "guest",
            Password = _config["RabbitMQ:Password"] ?? "guest",
            Port = int.TryParse(_config["RabbitMQ:Port"], out var p) ? p : 5672,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Main exchange
        _channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout, durable: true);

        // DLX + DLQ
        _channel.ExchangeDeclare(exchange: dlxExchange, type: ExchangeType.Direct, durable: true);
        _channel.QueueDeclare(queue: dlqName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        _channel.QueueBind(queue: dlqName, exchange: dlxExchange, routingKey: dlRoutingKey);

        // Main queue with DLX args
        var args = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = dlxExchange,
            ["x-dead-letter-routing-key"] = dlRoutingKey
        };

        _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
        _channel.QueueBind(queue: _queueName, exchange: exchange, routingKey: "");

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        _logger.LogInformation(
            "RabbitMQ consumer init OK. Exchange={Exchange} Queue={Queue} DLX={DLX} DLQ={DLQ} Host={Host}:{Port}",
            exchange, _queueName, dlxExchange, dlqName, factory.HostName, factory.Port);

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel == null)
            throw new Exception("RabbitMQ channel not initialized.");

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                using var doc = JsonDocument.Parse(json);
                var eventName = doc.RootElement.GetProperty("eventName").GetString();

                if (eventName != "LeaveRequestCreated")
                {
                    _channel!.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                var payloadJson = doc.RootElement.GetProperty("payload").GetRawText();
                var payload = JsonSerializer.Deserialize<LeaveRequestCreatedEvent>(payloadJson);

                if (payload == null)
                    throw new Exception("LeaveRequestCreated payload parse failed.");

                // ✅ iş mantığı handler'da
                try
                {
                    await _handler.HandleAsync(payload, stoppingToken);

                    // ✅ ACK tek yerde
                    _channel!.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[LeaveRequestCreated] Error while handling event. DeliveryTag={DeliveryTag}",
                        ea.DeliveryTag);

                    // ✅ İşleme hatası -> DLQ
                    _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ERROR] LeaveRequestEventsConsumer failed.");

                // ✅ Parse vb. kritik hata -> DLQ
                _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };


        _logger.LogInformation("Consuming started. Queue={Queue}", _queueName);

        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try { _channel?.Close(); } catch { }
        try { _connection?.Close(); } catch { }
        _channel?.Dispose();
        _connection?.Dispose();

        return base.StopAsync(cancellationToken);
    }

    private static List<int> ParsePathIds(string path)
    {
        return path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();
    }
}
