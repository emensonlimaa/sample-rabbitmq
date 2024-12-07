namespace MessageBroker.RabbitMq.Abstractions;

public interface IRabbitMqPublish
{
    Task Publish(string exchangeName, string routingKey, string exchangeType, string message);
}