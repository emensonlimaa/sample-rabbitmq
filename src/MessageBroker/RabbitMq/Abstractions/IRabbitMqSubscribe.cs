namespace MessageBroker.RabbitMq.Abstractions;

public interface IRabbitMqSubscribe
{
    Task Subscribe(string exchangeName, string routingKey, string exchangeType, string queueName,
        IEventHandler handler);
}