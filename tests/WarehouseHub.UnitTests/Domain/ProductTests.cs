using WarehouseHub.Domain.Common;
using WarehouseHub.Domain.Entities;

namespace WarehouseHub.UnitTests.Domain;

public class ProductTests
{
    [Fact]
    public void Constructor_NormalizesSkuToUpperCase()
    {
        var product = new Product("  kb-001 ", "Keyboard", 100m, 5);

        Assert.Equal("KB-001", product.Sku);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_RejectsNonPositivePrice(decimal price)
    {
        Assert.Throws<DomainException>(() => new Product("KB-001", "Keyboard", price, 5));
    }

    [Fact]
    public void Constructor_RejectsNegativeInitialStock()
    {
        Assert.Throws<DomainException>(() => new Product("KB-001", "Keyboard", 100m, -1));
    }

    [Fact]
    public void DecreaseStock_ReducesQuantity()
    {
        var product = new Product("KB-001", "Keyboard", 100m, 10);

        product.DecreaseStock(3);

        Assert.Equal(7, product.StockQuantity);
    }

    [Fact]
    public void DecreaseStock_MoreThanAvailable_ThrowsAndLeavesStockUnchanged()
    {
        var product = new Product("KB-001", "Keyboard", 100m, 2);

        var ex = Assert.Throws<DomainException>(() => product.DecreaseStock(3));

        Assert.Contains("Insufficient stock", ex.Message);
        Assert.Equal(2, product.StockQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IncreaseStock_RejectsNonPositiveQuantity(int quantity)
    {
        var product = new Product("KB-001", "Keyboard", 100m, 10);

        Assert.Throws<DomainException>(() => product.IncreaseStock(quantity));
    }
}
