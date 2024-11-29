namespace MessageBroker.RabbitMq.Abstractions;

public interface IEventHandler
{
    Task Handle(string message);
}