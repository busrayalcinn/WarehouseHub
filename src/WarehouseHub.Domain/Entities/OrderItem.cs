using WarehouseHub.Domain.Common;

namespace WarehouseHub.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal => Quantity * UnitPrice;

    private OrderItem() { } // Required by EF Core

    internal OrderItem(Guid productId, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    internal void IncreaseQuantity(int quantity) => Quantity += quantity;
}
