using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OT.Assessment.Consumer.Messaging;
using OT.Assessment.Consumer.Repositories;
using OT.Assessment.Contracts.Message;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OT.Assessment.Consumer;

public sealed class Worker(
    ILogger<Worker> logger,
    IOptions<RabbitMqOptions> options,
    IServiceScopeFactory scopeFactory)
    : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        try
        {

            logger.LogInformation(
    """
    RabbitMQ connection configuration:
    Host: {Host}
    Port: {Port}
    Username: {Username}
    VirtualHost: {VirtualHost}
    PasswordLength: {PasswordLength}
    PasswordMatchesExpected: {PasswordMatches}
    """,
    _options.HostName,
    _options.Port,
    _options.UserName,
    _options.VirtualHost,
    _options.Password.Length,
    _options.Password == "otpassword123");
            await using var connection =
                await factory.CreateConnectionAsync(stoppingToken);

            await using var channel =
                await connection.CreateChannelAsync(
                    cancellationToken: stoppingToken);

            await channel.QueueDeclareAsync(
                queue: _options.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            await channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (_, eventArgs) =>
            {
                try
                {
                    var message =
                        JsonSerializer.Deserialize<CasinoWagerMessage>(
                            eventArgs.Body.Span);

                    if (message is null)
                    {
                        logger.LogWarning(
                            "Received an empty or invalid wager message.");

                        await channel.BasicNackAsync(
                            eventArgs.DeliveryTag,
                            multiple: false,
                            requeue: false);

                        return;
                    }

                    logger.LogInformation(
                        "Processing wager {WagerId}, Amount: {Amount}",
                        message.WagerId,
                        message.Amount);

                    // Create a new dependency injection scope.
                    await using var scope =
                        scopeFactory.CreateAsyncScope();

                    var repository = scope.ServiceProvider
                        .GetRequiredService<IWagerRepository>();

                    // Persist the wager using Dapper.
                    var wasInserted = await repository.InsertAsync(
                        message,
                        stoppingToken);

                    // Acknowledge only after successful database processing.
                    await channel.BasicAckAsync(
                        eventArgs.DeliveryTag,
                        multiple: false);

                    if (wasInserted)
                    {
                        logger.LogInformation(
                            "Successfully inserted wager {WagerId}",
                            message.WagerId);
                    }
                    else
                    {
                        logger.LogInformation(
                            "Wager {WagerId} already exists. Duplicate ignored.",
                            message.WagerId);
                    }
                }
                catch (JsonException exception)
                {
                    logger.LogError(
                        exception,
                        "Invalid wager message received.");

                    await channel.BasicNackAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: false);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    logger.LogInformation(
                        "Wager processing cancelled during shutdown.");
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Failed to process wager. Message will be requeued.");

                    await channel.BasicNackAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: true);
                }
            };

            await channel.BasicConsumeAsync(
                queue: _options.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            logger.LogInformation(
                "Consumer listening on queue {QueueName}",
                _options.QueueName);

            await Task.Delay(
                Timeout.Infinite,
                stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Consumer is stopping.");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Consumer encountered an unexpected error.");

            throw;
        }
    }
}