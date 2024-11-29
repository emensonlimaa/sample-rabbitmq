namespace MessageBroker.RabbitMq.Abstractions;

public interface IRabbitMqSubscribe : IDisposable
{
    void Subscribe(string exchangeName, string routingKey, string exchangeType, string queueName,
        IEventHandler handler);
}