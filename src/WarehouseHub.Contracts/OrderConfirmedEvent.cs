namespace WarehouseHub.Contracts;

public record OrderConfirmedEvent(
    Guid OrderId,
    string CustomerName,
    DateTime ConfirmedAtUtc,
    IReadOnlyList<OrderConfirmedItem> Items);

public record OrderConfirmedItem(Guid ProductId, int Quantity);
