using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseHub.Application.Common.Exceptions;
using WarehouseHub.Application.Common.Interfaces;
using WarehouseHub.Domain.Enums;

namespace WarehouseHub.Application.Orders.Commands;

public record CancelOrderCommand(Guid OrderId) : IRequest;

public class CancelOrderCommandHandler(IApplicationDbContext db) : IRequestHandler<CancelOrderCommand>
{
    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException($"Order '{request.OrderId}' was not found.");

        var wasConfirmed = order.Status == OrderStatus.Confirmed;
        order.Cancel();

        // Stock was only taken when the order was confirmed, so only then is it returned
        if (wasConfirmed)
        {
            var productIds = order.Items.Select(i => i.ProductId).ToList();
            var products = await db.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            foreach (var item in order.Items)
                products[item.ProductId].IncreaseStock(item.Quantity);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
