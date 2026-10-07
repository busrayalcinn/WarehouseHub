using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseHub.Application.Common.Interfaces;

namespace WarehouseHub.Application.Orders.Queries;

public record GetOrdersQuery : IRequest<IReadOnlyList<OrderSummaryDto>>;

public class GetOrdersQueryHandler(IApplicationDbContext db) : IRequestHandler<GetOrdersQuery, IReadOnlyList<OrderSummaryDto>>
{
    public async Task<IReadOnlyList<OrderSummaryDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        return await db.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.CustomerName,
                o.Status,
                o.CreatedAtUtc,
                o.Items.Count(),
                o.Items.Sum(i => i.Quantity * i.UnitPrice)))
            .ToListAsync(cancellationToken);
    }
}
