using WarehouseHub.Domain.Common;
using WarehouseHub.Domain.Enums;

namespace WarehouseHub.Domain.Entities;

public class Order : BaseEntity
{
    private readonly List<OrderItem> _items = new();

    public string CustomerName { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public decimal TotalAmount => _items.Sum(i => i.LineTotal);

    private Order() { } // Required by EF Core

    public Order(string customerName)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("Customer name cannot be empty.");

        CustomerName = customerName.Trim();
        Status = OrderStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void AddItem(Product product, int quantity)
    {
        EnsurePending();

        if (product is null)
            throw new DomainException("Product is required.");
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing is not null)
            existing.IncreaseQuantity(quantity);
        else
            _items.Add(new OrderItem(product.Id, quantity, product.UnitPrice));
    }

    public void Confirm()
    {
        EnsurePending();

        if (_items.Count == 0)
            throw new DomainException("An order without items cannot be confirmed.");

        Status = OrderStatus.Confirmed;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            throw new DomainException("Order is already cancelled.");

        Status = OrderStatus.Cancelled;
    }

    private void EnsurePending()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException($"Order is {Status}; only pending orders can be modified.");
    }
}
