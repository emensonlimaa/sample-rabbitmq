using System.Text;
using MessageBroker.Configuration;
using MessageBroker.RabbitMq.Abstractions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MessageBroker.RabbitMq;

public  class RabbitMqClient : IRabbitMqClient
{
    private static readonly string AppName = AppDomain.CurrentDomain.FriendlyName;
    private readonly IConnectionFactory _factory = GetConnectionFactory();
    private readonly object _lockObject = new();
    private readonly ILogger _logger;
    private static readonly string MachineName = Environment.MachineName;
    private IModel? _channel;
    private IConnection? _connection;
    private bool _disposed;

    public RabbitMqClient(ILogger<RabbitMqClient> logger)
    {
        _logger = logger;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private static IConnectionFactory GetConnectionFactory()
    {
        var settings = AppSettingsReader.GetSection<RabbitMqConfiguration>("RabbitMq");

        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            UserName = settings.UserName,
            Password = settings.Password,
            VirtualHost = settings.VirtualHost,
            Port = settings.Port,
            ClientProvidedName = $"App: {AppName}, Machine: {MachineName}",
            RequestedConnectionTimeout = TimeSpan.FromMicroseconds(settings.RequestedConnectionTimeout),
            SocketReadTimeout = TimeSpan.FromMicroseconds(settings.SocketReadTimeout),
            SocketWriteTimeout = TimeSpan.FromMicroseconds(settings.SocketWriteTimeout),
            AutomaticRecoveryEnabled = settings.AutomaticRecoveryEnabled,
            NetworkRecoveryInterval = TimeSpan.FromMicroseconds(settings.NetworkRecoveryInterval),
            TopologyRecoveryEnabled = settings.TopologyRecoveryEnabled,
            DispatchConsumersAsync = settings.DispatchConsumersAsync
        };
        
        return factory;
    }

    public IModel? GetOrCreateChannel()
    {
        if (_disposed)
            throw new ObjectDisposedException("RabbitMqClient", "Attempting to use a disposed RabbitMqClient.");

        lock (_lockObject)
        {
            if (_channel is { IsOpen: true })
                return _channel;

            if (_connection is { IsOpen: true })
                _connection = CreateConnection();
 
            _channel = _connection.CreateModel();

            RegisterChannelEvents();
            _logger.LogInformation("RabbitMQ channel created successfully.");
            return _channel;
        }
    }

    private IConnection? CreateConnection()
    {
        lock (_lockObject)
        {
            var connection = _factory.CreateConnection();
            RegisterConnectionEvents(connection);
            _logger.LogInformation("RabbitMQ connection established successfully.");
            return connection;
        }
    }

    private void RegisterConnectionEvents(IConnection connection)
    {
        connection.ConnectionShutdown += HandleShutdownEvent;
        connection.CallbackException += HandleCallbackException;
        connection.ConnectionBlocked += HandleBlockedEvent;
        connection.ConnectionUnblocked += HandleUnblockedEvent;
    }

    private void RegisterChannelEvents()
    {
        _channel.ModelShutdown += HandleShutdownEvent;
        _channel.CallbackException += HandleCallbackException;
        _channel.BasicReturn += HandleBasicReturn;
    }

    private void HandleShutdownEvent(object? sender, ShutdownEventArgs e)
    {
        _logger.LogWarning(new StringBuilder().Append("RabbitMQ shutdown: ").Append(e.ReplyText).ToString());
    }

    private void HandleCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        _logger.LogError(new StringBuilder().Append("RabbitMQ callback exception: ")
            .Append(e.Exception.Message)
            .ToString());
    }

    private void HandleBlockedEvent(object? sender, ConnectionBlockedEventArgs e)
    {
        _logger.LogWarning(new StringBuilder().Append("RabbitMQ connection blocked: ").Append(e.Reason).ToString());
    }

    private void HandleUnblockedEvent(object? sender, EventArgs e)
    {
        _logger.LogInformation("RabbitMQ connection unblocked.");
    }

    private void HandleBasicReturn(object? sender, BasicReturnEventArgs e)
    {
        _logger.LogWarning(new StringBuilder().Append("RabbitMQ message return: ").Append(e.ReplyText).ToString());
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
            lock (_lockObject)
            {
                _channel?.Close();
                _connection?.Close();
                _channel?.Dispose();
                _connection?.Dispose();
                _channel = null;
                _connection = null;
            }

        _disposed = true;
    }
}