
using Microsoft.Extensions.Options;
using OT.Assessment.Contracts.Message;
using RabbitMQ.Client;
using System.Text.Json;

namespace OT.Assessment.App.Messaging;

public sealed class RabbitMqPublisher : IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private IConnection? _connection;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;

        _connectionFactory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };
    }

    public async Task PublishAsync(
        CasinoWagerMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.WagerId))
            throw new ArgumentException("WagerId is required.");

        var connection = await GetConnectionAsync(cancellationToken);

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

        await using var channel = await connection.CreateChannelAsync(
            channelOptions,
            cancellationToken);

        await channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = message.WagerId,
            Type = nameof(CasinoWagerMessage),
            Timestamp = new AmqpTimestamp(
                DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _options.QueueName,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    private async Task<IConnection> GetConnectionAsync(
        CancellationToken cancellationToken)
    {
        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

            _connection = await _connectionFactory.CreateConnectionAsync(
                cancellationToken);

            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();

        _connectionLock.Dispose();
    }
}
