using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WarehouseHub.Application.Common.Interfaces;
using WarehouseHub.Infrastructure.Caching;
using WarehouseHub.Infrastructure.Messaging;
using WarehouseHub.Infrastructure.Persistence;

namespace WarehouseHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("WarehouseDb")
            ?? throw new InvalidOperationException("Connection string 'WarehouseDb' is missing.");
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is missing.");
        var rabbitMqConnection = configuration.GetConnectionString("RabbitMq")
            ?? throw new InvalidOperationException("Connection string 'RabbitMq' is missing.");

        services.AddDbContext<WarehouseDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<WarehouseDbContext>());

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "warehousehub:";
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddSingleton<IEventPublisher>(sp => new RabbitMqEventPublisher(
            rabbitMqConnection,
            sp.GetRequiredService<ILogger<RabbitMqEventPublisher>>()));

        return services;
    }
}
