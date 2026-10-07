using RabbitMQ.Client;

namespace WarehouseHub.Messaging;

public static class RabbitMqTopology
{
    public const string Exchange = "warehousehub";
    public const string OrderConfirmedRoutingKey = "order-confirmed";
    public const string OrderConfirmedQueue = "order-confirmed";
    public const string OrderConfirmedRetryQueue = "order-confirmed.retry";
    public const string OrderConfirmedDeadLetterQueue = "order-confirmed.dlq";
    public const string RetryCountHeader = "x-retry-count";
    public const int RetryDelayMs = 5000;
    public const int MaxRetries = 3;

    // Declarations are idempotent, so both the API and the worker can safely run this on startup
    public static async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken = default)
    {
        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Direct, durable: true, autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(OrderConfirmedQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: null, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(OrderConfirmedQueue, Exchange, OrderConfirmedRoutingKey,
            cancellationToken: cancellationToken);

        // Failed messages wait here, then expire back into the main queue for another attempt
        await channel.QueueDeclareAsync(OrderConfirmedRetryQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-message-ttl"] = RetryDelayMs,
                ["x-dead-letter-exchange"] = Exchange,
                ["x-dead-letter-routing-key"] = OrderConfirmedRoutingKey
            },
            cancellationToken: cancellationToken);

        // Messages that keep failing end up here for manual inspection
        await channel.QueueDeclareAsync(OrderConfirmedDeadLetterQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: null, cancellationToken: cancellationToken);
    }
}
