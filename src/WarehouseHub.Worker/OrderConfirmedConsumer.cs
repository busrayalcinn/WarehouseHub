using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WarehouseHub.Contracts;
using WarehouseHub.Messaging;

namespace WarehouseHub.Worker;

public class OrderConfirmedConsumer(IConfiguration configuration, ILogger<OrderConfirmedConsumer> logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = configuration.GetConnectionString("RabbitMq")
            ?? throw new InvalidOperationException("Connection string 'RabbitMq' is missing.");

        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
        _connection = await ConnectWithRetryAsync(factory, stoppingToken);
        _channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            stoppingToken);
        await RabbitMqTopology.DeclareAsync(_channel, stoppingToken);

        // Take one message at a time; the next arrives only after the current one is acknowledged
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (_, ea) => HandleAsync(ea, stoppingToken);

        // autoAck: false - a message is removed only after we explicitly acknowledge it
        await _channel.BasicConsumeAsync(RabbitMqTopology.OrderConfirmedQueue, autoAck: false, consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("Listening on queue '{Queue}'", RabbitMqTopology.OrderConfirmedQueue);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        var channel = _channel!;
        var retryCount = GetRetryCount(ea.BasicProperties);
        var body = ea.Body.ToArray();

        try
        {
            var message = JsonSerializer.Deserialize<OrderConfirmedEvent>(body)
                ?? throw new InvalidOperationException("Message body is empty.");

            await ProcessAsync(message, cancellationToken);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception ex)
        {
            if (retryCount < RabbitMqTopology.MaxRetries)
            {
                logger.LogWarning("Processing failed (attempt {Attempt}): {Error}. Retrying in {Delay} ms.",
                    retryCount + 1, ex.Message, RabbitMqTopology.RetryDelayMs);
                await RepublishAsync(channel, RabbitMqTopology.OrderConfirmedRetryQueue, body, ea.BasicProperties,
                    retryCount + 1, cancellationToken);
            }
            else
            {
                logger.LogError("Processing failed after {Attempts} attempts: {Error}. Moving message to the dead letter queue.",
                    retryCount + 1, ex.Message);
                await RepublishAsync(channel, RabbitMqTopology.OrderConfirmedDeadLetterQueue, body, ea.BasicProperties,
                    retryCount, cancellationToken);
            }

            // The copy is now safely stored in the retry or dead letter queue, so the original can be removed.
            // If republishing itself throws, the original stays unacknowledged and RabbitMQ redelivers it.
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken);
        }
    }

    private Task ProcessAsync(OrderConfirmedEvent message, CancellationToken cancellationToken)
    {
        // Simulated failure so the retry and dead letter flow can be demonstrated
        if (message.CustomerName.Contains("fail", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Simulated failure for order {message.OrderId}.");

        var totalUnits = message.Items.Sum(i => i.Quantity);
        logger.LogInformation("Preparing shipment for order {OrderId} ({Customer}): {Lines} lines, {Units} units",
            message.OrderId, message.CustomerName, message.Items.Count, totalUnits);

        return Task.CompletedTask;
    }

    private static async Task RepublishAsync(IChannel channel, string queue, byte[] body,
        IReadOnlyBasicProperties original, int retryCount, CancellationToken cancellationToken)
    {
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = original.ContentType,
            MessageId = original.MessageId,
            Headers = new Dictionary<string, object?> { [RabbitMqTopology.RetryCountHeader] = retryCount }
        };

        // The default exchange ("") routes straight to the queue with the same name
        await channel.BasicPublishAsync(string.Empty, queue, mandatory: true, basicProperties: properties,
            body: body, cancellationToken: cancellationToken);
    }

    private static int GetRetryCount(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers is not null
            && properties.Headers.TryGetValue(RabbitMqTopology.RetryCountHeader, out var value)
            && value is not null)
        {
            return Convert.ToInt32(value);
        }

        return 0;
    }

    private async Task<IConnection> ConnectWithRetryAsync(ConnectionFactory factory, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await factory.CreateConnectionAsync(cancellationToken);
            }
            catch (Exception ex) when (attempt < 10 && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("RabbitMQ not reachable (attempt {Attempt}): {Error}. Retrying in 3 s.", attempt, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
