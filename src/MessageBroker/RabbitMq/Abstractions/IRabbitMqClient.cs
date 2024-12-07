using RabbitMQ.Client;

namespace MessageBroker.RabbitMq.Abstractions;

public interface IRabbitMqClient
{
    Task<IChannel?> GetOrCreateChannel();
}