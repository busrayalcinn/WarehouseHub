using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseHub.Application.Common.Exceptions;
using WarehouseHub.Application.Common.Interfaces;
using WarehouseHub.Domain.Entities;

namespace WarehouseHub.Application.Orders.Commands;

public record CreateOrderItem(Guid ProductId, int Quantity);

public record CreateOrderCommand(string CustomerName, IReadOnlyList<CreateOrderItem> Items) : IRequest<Guid>;

public class CreateOrderCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count == 0)
            throw new RequestValidationException("An order must contain at least one item.");

        var order = new Order(request.CustomerName);

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                throw new NotFoundException($"Product '{item.ProductId}' was not found.");

            order.AddItem(product, item.Quantity);
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}
