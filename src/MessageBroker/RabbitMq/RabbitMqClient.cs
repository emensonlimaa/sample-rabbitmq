using MessageBroker.Configuration;
using MessageBroker.RabbitMq.Abstractions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MessageBroker.RabbitMq;

public class RabbitMqClient : IRabbitMqClient, IAsyncDisposable
{
    private static readonly string AppName = AppDomain.CurrentDomain.FriendlyName;
    private readonly ConnectionFactory _factory;
    private readonly ILogger _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;

    public RabbitMqClient(ILogger<RabbitMqClient> logger)
    {
        _logger = logger;
        _factory = GetConnectionFactory();
    }

    private static ConnectionFactory GetConnectionFactory()
    {
        var settings = AppSettingsReader.GetSection<RabbitMqConfiguration>("RabbitMq");

        return new ConnectionFactory
        {
            HostName = settings.HostName,
            UserName = settings.UserName,
            Password = settings.Password,
            VirtualHost = settings.VirtualHost,
            Port = settings.Port,
            ClientProvidedName = $"App: {AppName}, Machine: {Environment.MachineName}",
            RequestedConnectionTimeout = TimeSpan.FromMilliseconds(settings.RequestedConnectionTimeout),
            SocketReadTimeout = TimeSpan.FromMilliseconds(settings.SocketReadTimeout),
            SocketWriteTimeout = TimeSpan.FromMilliseconds(settings.SocketWriteTimeout),
            AutomaticRecoveryEnabled = settings.AutomaticRecoveryEnabled,
            NetworkRecoveryInterval = TimeSpan.FromMilliseconds(settings.NetworkRecoveryInterval),
            TopologyRecoveryEnabled = settings.TopologyRecoveryEnabled
        };
    }

    public async Task<IChannel?> GetOrCreateChannel()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMqClient), "Attempting to use a disposed RabbitMqClient.");

        if (_channel != null && _channel.IsOpen)
            return _channel;

        if (_connection == null || !_connection.IsOpen)
            _connection = await CreateConnection();

        _channel = await _connection.CreateChannelAsync();

        _logger.LogInformation($"RabbitMQ channel: {_channel.ChannelNumber} created successfully. ");
        return _channel;
    }

    private async Task<IConnection> CreateConnection()
    {
        _logger.LogInformation("Establishing RabbitMQ connection...");
        var connection = await _factory.CreateConnectionAsync();

        connection.ConnectionShutdownAsync += async (sender, e) =>
        {
            await HandleConnectionShutdown(e);
        };

        connection.CallbackExceptionAsync += async (sender, e) =>
        {
            await HandleCallbackException(e);
        };

        connection.ConnectionBlockedAsync += async (sender, e) =>
        {
            await HandleConnectionBlocked(e);
        };

        connection.ConnectionUnblockedAsync += async (sender, e) =>
        {
            await HandleConnectionUnblocked();
        };

        _logger.LogInformation($"RabbitMQ connection established successfully: {_connection?.ClientProvidedName}");
        return connection;
    }

    private async Task HandleConnectionShutdown(ShutdownEventArgs e)
    {
        _logger.LogWarning($"RabbitMQ connection shutdown: {e.ReplyText} - {_connection?.ClientProvidedName}");
        await Task.CompletedTask;
    }

    private async Task HandleCallbackException(CallbackExceptionEventArgs e)
    {
        _logger.LogError($"RabbitMQ callback exception: {e.Exception.Message} - {_connection?.ClientProvidedName}");
        await Task.CompletedTask;
    }

    private async Task HandleConnectionBlocked(ConnectionBlockedEventArgs e)
    {
        _logger.LogWarning($"RabbitMQ connection blocked: {e.Reason} - {_connection?.ClientProvidedName}");
        await Task.CompletedTask;
    }

    private async Task HandleConnectionUnblocked()
    {
        _logger.LogInformation($"RabbitMQ connection unblocked. {_connection?.ClientProvidedName}");
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_channel != null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        _channel = null;
        _connection = null;
        _disposed = true;
        _logger.LogInformation("RabbitMQ resources disposed successfully.");
    }
}