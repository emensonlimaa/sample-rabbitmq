using MessageBroker.RabbitMq.Abstractions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace MessageBroker.RabbitMq;

public class RabbitMqSubscribe : IRabbitMqSubscribe
{
    private readonly ILogger<RabbitMqSubscribe> _logger;
    private readonly IRabbitMqClient _rabbitMqClient;
    private IChannel? _channel;

    protected RabbitMqSubscribe(IRabbitMqClient rabbitMqClient, ILogger<RabbitMqSubscribe> logger)
    {
        _rabbitMqClient = rabbitMqClient;
        _logger = logger;
    }

    public async Task Subscribe(string exchangeName, string routingKey, string exchangeType, string queueName,
        IEventHandler handler)
    {
        try
        {
            _channel = await _rabbitMqClient.GetOrCreateChannel();

            await _channel!.QueueDeclareAsync(
                queueName,
                true,
                false,
                false,
                null
            );

            await _channel.BasicQosAsync(0, 1, false);

            await _channel.ExchangeDeclareAsync(
                exchangeName,
                exchangeType,
                true,
                false,
                null
            );

            await _channel.QueueBindAsync(
                queueName,
                exchangeName,
                routingKey
            );

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                try
                {
                    await handler.Handle(message);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message. NACKing the message. MessageId: {MessageId}",
                        ea.BasicProperties.MessageId);
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            await _channel.BasicConsumeAsync(queueName, false, consumer);
            _logger.LogInformation("Subscription to {QueueName} is set up successfully.", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set up subscription for {QueueName}.", queueName);
            throw;
        }
    }
}