using System.Text.Json;
using Microsoft.Extensions.Options;
using OT.Assessment.Consumer.Messaging;
using OT.Assessment.Contracts.Message;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OT.Assessment.Consumer;

public sealed class Worker(
    ILogger<Worker> logger,
    IOptions<RabbitMqOptions> options)
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

                    // Database persistence will be added next.

                    await channel.BasicAckAsync(
                        eventArgs.DeliveryTag,
                        multiple: false);

                    logger.LogInformation(
                        "Successfully processed wager {WagerId}",
                        message.WagerId);
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