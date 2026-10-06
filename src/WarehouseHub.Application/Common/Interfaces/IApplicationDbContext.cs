using Microsoft.EntityFrameworkCore;
using WarehouseHub.Domain.Entities;

namespace WarehouseHub.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Order> Orders { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
