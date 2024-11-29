namespace MessageBroker.Configuration;

public class RabbitMqConfiguration
{
    public string HostName { get; set; } = "localhost";
    public string UserName { get; set; } = "admin";
    public string Password { get; set; } = "admin";
    public string VirtualHost { get; set; } = "/";
    public int Port { get; set; } = 5672;
    public long RequestedConnectionTimeout { get; set; } = 1000;
    public long SocketReadTimeout { get; set; } = 1000;
    public long SocketWriteTimeout { get; set; } = 1000;
    public bool AutomaticRecoveryEnabled { get; set; } = true;
    public long NetworkRecoveryInterval { get; set; }  = 1000;
    public bool TopologyRecoveryEnabled { get; set; } = true;
    public bool DispatchConsumersAsync { get; set; } = true;
}