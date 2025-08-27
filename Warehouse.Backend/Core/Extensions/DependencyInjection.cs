using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Warehouse.Backend.Core.Infrastructure;

namespace Warehouse.Backend.Core.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreDependencies(this IServiceCollection services)
    {
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(Paths.GetDataProtectionPath()))
            .SetApplicationName("Warehouse")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(365));

        string connectionString = $"Data Source={Paths.GetDatabasePath()}";
        services.AddDbContext<WarehouseDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<WebSocketClient>();

        return services;
    }
}
