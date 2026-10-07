using WarehouseHub.Domain.Enums;

namespace WarehouseHub.Application.Orders;

public record OrderItemDto(Guid ProductId, int Quantity, decimal UnitPrice, decimal LineTotal);

public record OrderDto(
    Guid Id,
    string CustomerName,
    OrderStatus Status,
    DateTime CreatedAtUtc,
    decimal TotalAmount,
    IReadOnlyList<OrderItemDto> Items);

public record OrderSummaryDto(
    Guid Id,
    string CustomerName,
    OrderStatus Status,
    DateTime CreatedAtUtc,
    int ItemCount,
    decimal TotalAmount);
