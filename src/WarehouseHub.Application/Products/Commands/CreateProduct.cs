using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseHub.Application.Common.Exceptions;
using WarehouseHub.Application.Common.Interfaces;
using WarehouseHub.Domain.Entities;

namespace WarehouseHub.Application.Products.Commands;

public record CreateProductCommand(string Sku, string Name, decimal UnitPrice, int InitialStock) : IRequest<Guid>;

public class CreateProductCommandHandler(IApplicationDbContext db, ICacheService cache)
    : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product(request.Sku, request.Name, request.UnitPrice, request.InitialStock);

        var skuExists = await db.Products.AnyAsync(p => p.Sku == product.Sku, cancellationToken);
        if (skuExists)
            throw new ConflictException($"A product with SKU '{product.Sku}' already exists.");

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        // The cached list no longer includes the new product
        await cache.RemoveAsync([ProductCacheKeys.All], cancellationToken);

        return product.Id;
    }
}
