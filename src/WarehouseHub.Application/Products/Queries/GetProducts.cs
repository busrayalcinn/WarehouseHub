using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseHub.Application.Common.Interfaces;

namespace WarehouseHub.Application.Products.Queries;

public record GetProductsQuery : IRequest<IReadOnlyList<ProductDto>>;

public class GetProductsQueryHandler(IApplicationDbContext db) : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        return await db.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(p.Id, p.Sku, p.Name, p.UnitPrice, p.StockQuantity))
            .ToListAsync(cancellationToken);
    }
}
