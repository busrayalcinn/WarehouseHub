namespace WarehouseHub.Application.Products;

public static class ProductCacheKeys
{
    public const string All = "products:all";
    public static string ById(Guid id) => $"products:{id}";

    // TTL is a safety net: even if an invalidation is missed, stale data expires on its own
    public static readonly TimeSpan ListTtl = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan ItemTtl = TimeSpan.FromMinutes(10);

    // Keys to clear when the given products change: each product entry plus the list
    public static IEnumerable<string> ForProducts(IEnumerable<Guid> productIds)
        => productIds.Select(ById).Append(All);
}
