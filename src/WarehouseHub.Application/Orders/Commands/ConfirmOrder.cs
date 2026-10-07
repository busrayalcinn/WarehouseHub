using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseHub.Application.Common.Exceptions;
using WarehouseHub.Application.Common.Interfaces;
using WarehouseHub.Application.Products;

namespace WarehouseHub.Application.Orders.Commands;

public record ConfirmOrderCommand(Guid OrderId) : IRequest;

public class ConfirmOrderCommandHandler(IApplicationDbContext db, ICacheService cache)
    : IRequestHandler<ConfirmOrderCommand>
{
    public async Task Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException($"Order '{request.OrderId}' was not found.");

        // Checks that the order is pending and has items before touching any stock
        order.Confirm();

        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        // If any item lacks stock, DecreaseStock throws and nothing is saved
        foreach (var item in order.Items)
            products[item.ProductId].DecreaseStock(item.Quantity);

        // One SaveChanges call: order status and all stock changes are committed together
        await db.SaveChangesAsync(cancellationToken);

        // Invalidate only after the database commit succeeded
        await cache.RemoveAsync(ProductCacheKeys.ForProducts(productIds), cancellationToken);
    }
}
