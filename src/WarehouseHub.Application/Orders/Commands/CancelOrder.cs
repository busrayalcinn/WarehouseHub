using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseHub.Application.Common.Exceptions;
using WarehouseHub.Application.Common.Interfaces;
using WarehouseHub.Application.Products;
using WarehouseHub.Domain.Enums;

namespace WarehouseHub.Application.Orders.Commands;

public record CancelOrderCommand(Guid OrderId) : IRequest;

public class CancelOrderCommandHandler(IApplicationDbContext db, ICacheService cache)
    : IRequestHandler<CancelOrderCommand>
{
    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException($"Order '{request.OrderId}' was not found.");

        var wasConfirmed = order.Status == OrderStatus.Confirmed;
        order.Cancel();

        if (!wasConfirmed)
        {
            // Stock was never taken for a pending order, so there is nothing to return
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var item in order.Items)
            products[item.ProductId].IncreaseStock(item.Quantity);

        await db.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(ProductCacheKeys.ForProducts(productIds), cancellationToken);
    }
}
