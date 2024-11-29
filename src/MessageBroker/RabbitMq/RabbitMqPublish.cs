using System.Text;
using MessageBroker.RabbitMq.Abstractions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace MessageBroker.RabbitMq;

public class RabbitMqPublish :  IRabbitMqPublish 
{
    private readonly ILogger<RabbitMqPublish> _logger;
    private readonly IRabbitMqClient _rabbitMqClient;
    private IModel? _channel;

    protected RabbitMqPublish(IRabbitMqClient rabbitMqClient, ILogger<RabbitMqPublish> logger)
    {
        _rabbitMqClient = rabbitMqClient;
        _logger = logger;
    }

    public void Dispose()
    {
        _rabbitMqClient?.Dispose();
        _channel?.Close();
        _channel?.Dispose();
    }

    public void Publish(string exchangeName, string routingKey, string exchangeType, string message)
    {
        try
        {
            _channel = _rabbitMqClient.GetOrCreateChannel();

            _channel.ConfirmSelect();

            _channel.ExchangeDeclare(
                exchangeName,
                exchangeType,
                true,
                false,
                null
            );

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Priority = 0;
            properties.MessageId = Guid.NewGuid().ToString();
            properties.DeliveryMode = 2;

            _channel.BasicPublish(
                exchangeName,
                routingKey,
                properties,
                Encoding.UTF8.GetBytes(message)
            );

            var confirmReceived = _channel.WaitForConfirms(TimeSpan.FromMilliseconds(250));

            if (!confirmReceived)
            {
                _logger.LogError("Failed to receive confirmation for message: {MessageId}", properties.MessageId);
                throw new Exception("RabbitMQ channel confirmation failed.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message to RabbitMQ.");
            throw;
        }
    }
}