using WarehouseHub.Domain.Common;

namespace WarehouseHub.Domain.Entities;

public class Product : BaseEntity
{
    public string Sku { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }

    private Product() { } // Required by EF Core

    public Product(string sku, string name, decimal unitPrice, int initialStock = 0)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("SKU cannot be empty.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name cannot be empty.");
        if (unitPrice <= 0)
            throw new DomainException("Unit price must be greater than zero.");
        if (initialStock < 0)
            throw new DomainException("Initial stock cannot be negative.");

        Sku = sku.Trim().ToUpperInvariant();
        Name = name.Trim();
        UnitPrice = unitPrice;
        StockQuantity = initialStock;
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        StockQuantity += quantity;
    }

    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");
        if (quantity > StockQuantity)
            throw new DomainException(
                $"Insufficient stock for {Sku}: available {StockQuantity}, requested {quantity}.");

        StockQuantity -= quantity;
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new DomainException("Unit price must be greater than zero.");

        UnitPrice = newPrice;
    }
}
