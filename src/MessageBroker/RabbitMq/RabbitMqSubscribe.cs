using System.Text;
using MessageBroker.RabbitMq.Abstractions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MessageBroker.RabbitMq;

public class RabbitMqSubscribe : IRabbitMqSubscribe
{
    private readonly ILogger<RabbitMqSubscribe> _logger;
    private readonly IRabbitMqClient _rabbitMqClient;
    private IModel? _channel;

    protected RabbitMqSubscribe(IRabbitMqClient rabbitMqClient, ILogger<RabbitMqSubscribe> logger)
    {
        _rabbitMqClient = rabbitMqClient;
        _logger = logger;
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _logger.LogInformation("RabbitMQ channel disposed.");
    }

    public void Subscribe(string exchangeName, string routingKey, string exchangeType, string queueName,
        IEventHandler handler)
    {
        try
        {
            _channel = _rabbitMqClient.GetOrCreateChannel();

            _channel.QueueDeclare(
                queueName,
                true,
                false,
                false,
                null
            );

            _channel.BasicQos(0, 1, false);

            _channel.ExchangeDeclare(
                exchangeName,
                exchangeType,
                true,
                false,
                null
            );

            _channel.QueueBind(
                queueName,
                exchangeName,
                routingKey
            );

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                try
                {
                    handler.Handle(message);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message. NACKing the message. MessageId: {MessageId}",
                        ea.BasicProperties.MessageId);
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(queueName, false, consumer);
            _logger.LogInformation("Subscription to {QueueName} is set up successfully.", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set up subscription for {QueueName}.", queueName);
            throw;
        }
    }
}