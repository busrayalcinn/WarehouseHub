using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseHub.Application.Common.Exceptions;
using WarehouseHub.Application.Common.Interfaces;

namespace WarehouseHub.Application.Products.Queries;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;

public class GetProductByIdQueryHandler(IApplicationDbContext db, ICacheService cache)
    : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var key = ProductCacheKeys.ById(request.Id);

        var cached = await cache.GetAsync<ProductDto>(key, cancellationToken);
        if (cached is not null)
            return cached;

        var product = await db.Products
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .Select(p => new ProductDto(p.Id, p.Sku, p.Name, p.UnitPrice, p.StockQuantity))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Product '{request.Id}' was not found.");

        await cache.SetAsync(key, product, ProductCacheKeys.ItemTtl, cancellationToken);
        return product;
    }
}
