using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using WarehouseHub.Application.Common.Interfaces;
using WarehouseHub.Contracts;
using WarehouseHub.Messaging;

namespace WarehouseHub.Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher(string connectionString, ILogger<RabbitMqEventPublisher> logger)
    : IEventPublisher, IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishOrderConfirmedAsync(OrderConfirmedEvent message, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var channel = await GetChannelAsync(cancellationToken);
            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            var properties = new BasicProperties
            {
                Persistent = true, // survives a broker restart
                ContentType = "application/json",
                MessageId = message.OrderId.ToString()
            };

            // With publisher confirms enabled, this completes only after the broker has stored the message
            await channel.BasicPublishAsync(
                RabbitMqTopology.Exchange,
                RabbitMqTopology.OrderConfirmedRoutingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            logger.LogInformation("Published OrderConfirmed event for order {OrderId}", message.OrderId);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        if (_connection is null || !_connection.IsOpen)
        {
            var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
            _connection = await factory.CreateConnectionAsync(cancellationToken);
        }

        _channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);
        await RabbitMqTopology.DeclareAsync(_channel, cancellationToken);

        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
