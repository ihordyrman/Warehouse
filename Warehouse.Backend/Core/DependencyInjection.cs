using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Warehouse.Backend.Core.Infrastructure;

namespace Warehouse.Backend.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreDependencies(this IServiceCollection services)
    {
        services.AddDataProtection(x => x.ApplicationDiscriminator = "Warehouse")
            .PersistKeysToFileSystem(new DirectoryInfo(Paths.GetDataProtectionPath()))
            .SetApplicationName("Warehouse.Backend")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(365));

        string connectionString = $"Data Source={Paths.GetDatabasePath()}";
        services.AddDbContext<WarehouseDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<WebSocketClient>();

        return services;
    }

    public static async Task<IApplicationBuilder> EnsureDbReadinessAsync(this WebApplication app)
    {
        IServiceScopeFactory? scopeFactory = app.Services.GetService<IServiceScopeFactory>();
        using IServiceScope scope = scopeFactory!.CreateScope();
        WarehouseDbContext? dbContext = scope.ServiceProvider.GetService<WarehouseDbContext>();

        if (await dbContext!.Database.CanConnectAsync())
        {
            IEnumerable<string> pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                await dbContext.Database.MigrateAsync();
            }
        }
        else
        {
            await dbContext.Database.MigrateAsync();
        }

        return app;
    }
}
