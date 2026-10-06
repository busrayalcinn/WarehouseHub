using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseHub.Application.Common.Exceptions;
using WarehouseHub.Application.Common.Interfaces;

namespace WarehouseHub.Application.Products.Queries;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;

public class GetProductByIdQueryHandler(IApplicationDbContext db) : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        return await db.Products
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .Select(p => new ProductDto(p.Id, p.Sku, p.Name, p.UnitPrice, p.StockQuantity))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Product '{request.Id}' was not found.");
    }
}
