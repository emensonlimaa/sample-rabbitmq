namespace MessageBroker.RabbitMq.Abstractions;

public interface IRabbitMqPublish : IDisposable
{
    void Publish(string exchangeName, string routingKey, string exchangeType, string message);
}