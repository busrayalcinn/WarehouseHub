using WarehouseHub.Application.Common.Interfaces;
using WarehouseHub.Contracts;

namespace WarehouseHub.UnitTests.TestHelpers;

public sealed class FakeCacheService : ICacheService
{
    public List<string> RemovedKeys { get; } = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        => Task.FromResult<T?>(default);

    public Task SetAsync<T>(string key, T value, TimeSpan timeToLive, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        RemovedKeys.AddRange(keys);
        return Task.CompletedTask;
    }
}

public sealed class FakeEventPublisher : IEventPublisher
{
    public List<OrderConfirmedEvent> Published { get; } = new();
    public bool ShouldFail { get; init; }

    public Task PublishOrderConfirmedAsync(OrderConfirmedEvent message, CancellationToken cancellationToken = default)
    {
        if (ShouldFail)
            throw new InvalidOperationException("Broker unavailable.");

        Published.Add(message);
        return Task.CompletedTask;
    }
}
