using Microsoft.EntityFrameworkCore;
using WarehouseHub.Domain.Entities;
using WarehouseHub.Infrastructure.Persistence;

namespace WarehouseHub.UnitTests.TestHelpers;

// Each test gets its own isolated in-memory database.
// A fresh context per step mimics separate HTTP requests, so tests read what was actually saved.
public sealed class TestDb
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    public WarehouseDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options);

    public async Task<(Product Product, Order Order)> SeedOrderAsync(int stock, int orderQuantity)
    {
        var product = new Product("KB-001", "Mechanical Keyboard", 1499.90m, stock);
        var order = new Order("Ayse Demir");
        order.AddItem(product, orderQuantity);

        await using var db = CreateContext();
        db.Products.Add(product);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return (product, order);
    }
}
