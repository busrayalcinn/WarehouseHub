using Microsoft.EntityFrameworkCore;
using WarehouseHub.Application.Common.Interfaces;
using WarehouseHub.Domain.Entities;

namespace WarehouseHub.Infrastructure.Persistence;

public class WarehouseDbContext : DbContext, IApplicationDbContext
{
    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WarehouseDbContext).Assembly);
    }
}
