using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WarehouseHub.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    // Applies pending migrations on startup. Retries because the database may still be starting,
    // even after its health check passes.
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WarehouseDbContext>>();

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Database migrations applied.");
                return;
            }
            catch (Exception ex) when (attempt < 10)
            {
                logger.LogWarning("Database not ready (attempt {Attempt}): {Error}. Retrying in 5 s.", attempt, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }
}
