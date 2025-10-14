using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Infrastructure.WebSockets;
using Warehouse.Core.Markets.Concrete.Okx.Services;
using Warehouse.Core.Markets.Contracts;
using Warehouse.Core.Markets.Services;
using Warehouse.Core.Orders.Contracts;
using Warehouse.Core.Orders.Services;
using Warehouse.Core.Shared.Services;
using Warehouse.Core.Workers.Contracts;
using Warehouse.Core.Workers.Services;

namespace Warehouse.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreDependencies(this IServiceCollection services)
    {
        services.AddDataProtection(x => x.ApplicationDiscriminator = "Warehouse")
            .PersistKeysToFileSystem(new DirectoryInfo(Directory.GetCurrentDirectory()))
            .SetApplicationName("Warehouse.Backend")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(365));

        // todo: while in local development
        services.AddDbContext<WarehouseDbContext>(options => options.UseNpgsql(
                                                      "Host=localhost;Database=warehouse;Username=postgres;Password=postgres"));

        services.AddScoped<WebSocketClient>();
        services.AddScoped<ICredentialsProvider, DatabaseCredentialsProvider>();
        services.AddScoped<ICandlestickService, CandlestickService>();
        services.AddScoped<IBalanceManager, BalanceManager>();
        services.AddScoped<IOrderManager, OrderManager>();
        services.AddSingleton<IWorkerManager, WorkerManager>();
        services.AddSingleton<IMarketDataCache, MarketDataCache>();
        return services;
    }
}
