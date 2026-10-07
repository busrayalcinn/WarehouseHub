using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseHub.Application.Common.Interfaces;

namespace WarehouseHub.Application.Products.Queries;

public record GetProductsQuery : IRequest<IReadOnlyList<ProductDto>>;

public class GetProductsQueryHandler(IApplicationDbContext db, ICacheService cache)
    : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync<List<ProductDto>>(ProductCacheKeys.All, cancellationToken);
        if (cached is not null)
            return cached;

        var products = await db.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(p.Id, p.Sku, p.Name, p.UnitPrice, p.StockQuantity))
            .ToListAsync(cancellationToken);

        await cache.SetAsync(ProductCacheKeys.All, products, ProductCacheKeys.ListTtl, cancellationToken);
        return products;
    }
}
