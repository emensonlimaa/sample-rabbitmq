using MessageBroker.RabbitMq.Abstractions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;

namespace MessageBroker.RabbitMq;

public class RabbitMqPublish :  IRabbitMqPublish 
{
    private readonly ILogger<RabbitMqPublish> _logger;
    private readonly IRabbitMqClient _rabbitMqClient;
    private IChannel? _channel;

    protected RabbitMqPublish(IRabbitMqClient rabbitMqClient, ILogger<RabbitMqPublish> logger)
    {
        _rabbitMqClient = rabbitMqClient;
        _logger = logger;
    }

    public async Task Publish(string exchangeName, string routingKey, string exchangeType, string message)
    {
        try
        {
            _channel = await _rabbitMqClient.GetOrCreateChannel();

            await _channel!.ExchangeDeclareAsync(
                exchangeName,
                exchangeType,
                durable: true,
                autoDelete: false
            );

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Priority = 0,
                MessageId = Guid.NewGuid().ToString(),
                DeliveryMode = DeliveryModes.Persistent
            };

            var body = Encoding.UTF8.GetBytes(message);

            await _channel.BasicPublishAsync(
                exchangeName,
                routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body
            );

            _logger.LogInformation("Message successfully published and confirmed. MessageId: {MessageId}", properties.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message to RabbitMQ.");
            throw;
        }
    }
}