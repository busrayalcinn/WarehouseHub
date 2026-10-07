using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseHub.Application.Common.Exceptions;
using WarehouseHub.Application.Common.Interfaces;

namespace WarehouseHub.Application.Orders.Queries;

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto>;

public class GetOrderByIdQueryHandler(IApplicationDbContext db) : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Order '{request.Id}' was not found.");

        return new OrderDto(
            order.Id,
            order.CustomerName,
            order.Status,
            order.CreatedAtUtc,
            order.TotalAmount,
            order.Items
                .Select(i => new OrderItemDto(i.ProductId, i.Quantity, i.UnitPrice, i.LineTotal))
                .ToList());
    }
}
