namespace WarehouseHub.Application.Common.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan timeToLive, CancellationToken cancellationToken = default);
    Task RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);
}
