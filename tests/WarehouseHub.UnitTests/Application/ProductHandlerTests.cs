using WarehouseHub.Application.Common.Exceptions;
using WarehouseHub.Application.Products;
using WarehouseHub.Application.Products.Commands;
using WarehouseHub.UnitTests.TestHelpers;

namespace WarehouseHub.UnitTests.Application;

public class ProductHandlerTests
{
    private readonly TestDb _testDb = new();
    private readonly FakeCacheService _cache = new();

    [Fact]
    public async Task CreateProduct_WithExistingSkuInDifferentCase_ThrowsConflict()
    {
        await using (var db = _testDb.CreateContext())
        {
            var handler = new CreateProductCommandHandler(db, _cache);
            await handler.Handle(new CreateProductCommand("kb-001", "Keyboard", 100m, 5), CancellationToken.None);
        }

        await using (var db = _testDb.CreateContext())
        {
            var handler = new CreateProductCommandHandler(db, _cache);
            var duplicate = new CreateProductCommand("KB-001", "Another Keyboard", 200m, 5);

            await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(duplicate, CancellationToken.None));
        }
    }

    [Fact]
    public async Task IncreaseStock_InvalidatesProductAndListCache()
    {
        Guid productId;
        await using (var db = _testDb.CreateContext())
        {
            var handler = new CreateProductCommandHandler(db, _cache);
            productId = await handler.Handle(new CreateProductCommand("kb-001", "Keyboard", 100m, 5), CancellationToken.None);
        }
        _cache.RemovedKeys.Clear();

        await using (var db = _testDb.CreateContext())
        {
            var handler = new IncreaseStockCommandHandler(db, _cache);
            await handler.Handle(new IncreaseStockCommand(productId, 10), CancellationToken.None);
        }

        Assert.Contains(ProductCacheKeys.All, _cache.RemovedKeys);
        Assert.Contains(ProductCacheKeys.ById(productId), _cache.RemovedKeys);
    }
}
