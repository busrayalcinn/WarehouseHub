using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseHub.Application.Common.Exceptions;
using WarehouseHub.Application.Common.Interfaces;

namespace WarehouseHub.Application.Products.Commands;

public record IncreaseStockCommand(Guid ProductId, int Quantity) : IRequest;

public class IncreaseStockCommandHandler(IApplicationDbContext db, ICacheService cache)
    : IRequestHandler<IncreaseStockCommand>
{
    public async Task Handle(IncreaseStockCommand request, CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException($"Product '{request.ProductId}' was not found.");

        product.IncreaseStock(request.Quantity);
        await db.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync(ProductCacheKeys.ForProducts([product.Id]), cancellationToken);
    }
}
