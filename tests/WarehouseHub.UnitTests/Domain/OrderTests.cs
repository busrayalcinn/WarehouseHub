using WarehouseHub.Domain.Common;
using WarehouseHub.Domain.Entities;
using WarehouseHub.Domain.Enums;

namespace WarehouseHub.UnitTests.Domain;

public class OrderTests
{
    private static Product CreateProduct(decimal price = 100m) => new("KB-001", "Keyboard", price, 10);

    [Fact]
    public void NewOrder_IsPending()
    {
        var order = new Order("Ayse Demir");

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Empty(order.Items);
    }

    [Fact]
    public void AddItem_SameProductTwice_MergesIntoOneLine()
    {
        var order = new Order("Ayse Demir");
        var product = CreateProduct();

        order.AddItem(product, 2);
        order.AddItem(product, 3);

        var item = Assert.Single(order.Items);
        Assert.Equal(5, item.Quantity);
        Assert.Equal(500m, order.TotalAmount);
    }

    [Fact]
    public void AddItem_KeepsPriceAtOrderTime()
    {
        var order = new Order("Ayse Demir");
        var product = CreateProduct(100m);
        order.AddItem(product, 2);

        product.UpdatePrice(150m);

        Assert.Equal(200m, order.TotalAmount);
    }

    [Fact]
    public void Confirm_WithoutItems_Throws()
    {
        var order = new Order("Ayse Demir");

        Assert.Throws<DomainException>(() => order.Confirm());
    }

    [Fact]
    public void Confirm_Twice_Throws()
    {
        var order = new Order("Ayse Demir");
        order.AddItem(CreateProduct(), 1);
        order.Confirm();

        Assert.Throws<DomainException>(() => order.Confirm());
    }

    [Fact]
    public void AddItem_AfterConfirm_Throws()
    {
        var order = new Order("Ayse Demir");
        var product = CreateProduct();
        order.AddItem(product, 1);
        order.Confirm();

        Assert.Throws<DomainException>(() => order.AddItem(product, 1));
    }

    [Fact]
    public void Cancel_Twice_Throws()
    {
        var order = new Order("Ayse Demir");
        order.Cancel();

        Assert.Throws<DomainException>(() => order.Cancel());
    }
}
