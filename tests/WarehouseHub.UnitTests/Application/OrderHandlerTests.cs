using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WarehouseHub.Application.Common.Exceptions;
using WarehouseHub.Application.Orders.Commands;
using WarehouseHub.Application.Products;
using WarehouseHub.Domain.Common;
using WarehouseHub.Domain.Enums;
using WarehouseHub.UnitTests.TestHelpers;

namespace WarehouseHub.UnitTests.Application;

public class OrderHandlerTests
{
    private readonly TestDb _testDb = new();
    private readonly FakeCacheService _cache = new();

    private async Task ConfirmAsync(Guid orderId, FakeEventPublisher? publisher = null)
    {
        await using var db = _testDb.CreateContext();
        var handler = new ConfirmOrderCommandHandler(db, _cache, publisher ?? new FakeEventPublisher(),
            NullLogger<ConfirmOrderCommandHandler>.Instance);
        await handler.Handle(new ConfirmOrderCommand(orderId), CancellationToken.None);
    }

    private async Task CancelAsync(Guid orderId)
    {
        await using var db = _testDb.CreateContext();
        var handler = new CancelOrderCommandHandler(db, _cache);
        await handler.Handle(new CancelOrderCommand(orderId), CancellationToken.None);
    }

    private async Task<(int Stock, OrderStatus Status)> ReadStateAsync(Guid productId, Guid orderId)
    {
        await using var db = _testDb.CreateContext();
        var stock = await db.Products.Where(p => p.Id == productId).Select(p => p.StockQuantity).SingleAsync();
        var status = await db.Orders.Where(o => o.Id == orderId).Select(o => o.Status).SingleAsync();
        return (stock, status);
    }

    [Fact]
    public async Task Confirm_DecreasesStock_InvalidatesCache_AndPublishesEvent()
    {
        var (product, order) = await _testDb.SeedOrderAsync(stock: 10, orderQuantity: 3);
        var publisher = new FakeEventPublisher();

        await ConfirmAsync(order.Id, publisher);

        var (stock, status) = await ReadStateAsync(product.Id, order.Id);
        Assert.Equal(7, stock);
        Assert.Equal(OrderStatus.Confirmed, status);
        Assert.Contains(ProductCacheKeys.All, _cache.RemovedKeys);
        Assert.Contains(ProductCacheKeys.ById(product.Id), _cache.RemovedKeys);
        var published = Assert.Single(publisher.Published);
        Assert.Equal(order.Id, published.OrderId);
    }

    [Fact]
    public async Task Confirm_WithInsufficientStock_SavesNothing()
    {
        var (product, order) = await _testDb.SeedOrderAsync(stock: 2, orderQuantity: 5);
        var publisher = new FakeEventPublisher();

        await Assert.ThrowsAsync<DomainException>(() => ConfirmAsync(order.Id, publisher));

        var (stock, status) = await ReadStateAsync(product.Id, order.Id);
        Assert.Equal(2, stock);
        Assert.Equal(OrderStatus.Pending, status);
        Assert.Empty(publisher.Published);
        Assert.Empty(_cache.RemovedKeys);
    }

    [Fact]
    public async Task Confirm_WhenPublishingFails_OrderStaysConfirmed()
    {
        var (product, order) = await _testDb.SeedOrderAsync(stock: 10, orderQuantity: 3);

        await ConfirmAsync(order.Id, new FakeEventPublisher { ShouldFail = true });

        var (stock, status) = await ReadStateAsync(product.Id, order.Id);
        Assert.Equal(7, stock);
        Assert.Equal(OrderStatus.Confirmed, status);
    }

    [Fact]
    public async Task Cancel_ConfirmedOrder_RestoresStock()
    {
        var (product, order) = await _testDb.SeedOrderAsync(stock: 10, orderQuantity: 3);
        await ConfirmAsync(order.Id);

        await CancelAsync(order.Id);

        var (stock, status) = await ReadStateAsync(product.Id, order.Id);
        Assert.Equal(10, stock);
        Assert.Equal(OrderStatus.Cancelled, status);
    }

    [Fact]
    public async Task Cancel_PendingOrder_DoesNotChangeStock()
    {
        var (product, order) = await _testDb.SeedOrderAsync(stock: 10, orderQuantity: 3);

        await CancelAsync(order.Id);

        var (stock, status) = await ReadStateAsync(product.Id, order.Id);
        Assert.Equal(10, stock);
        Assert.Equal(OrderStatus.Cancelled, status);
        Assert.Empty(_cache.RemovedKeys);
    }

    [Fact]
    public async Task Confirm_UnknownOrder_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => ConfirmAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateOrder_WithUnknownProduct_ThrowsNotFound()
    {
        await using var db = _testDb.CreateContext();
        var handler = new CreateOrderCommandHandler(db);
        var command = new CreateOrderCommand("Ayse Demir", [new CreateOrderItem(Guid.NewGuid(), 1)]);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateOrder_WithoutItems_ThrowsValidation()
    {
        await using var db = _testDb.CreateContext();
        var handler = new CreateOrderCommandHandler(db);
        var command = new CreateOrderCommand("Ayse Demir", []);

        await Assert.ThrowsAsync<RequestValidationException>(() => handler.Handle(command, CancellationToken.None));
    }
}
