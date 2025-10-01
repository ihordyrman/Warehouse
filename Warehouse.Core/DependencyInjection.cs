using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Core.Abstractions.Markets;
using Warehouse.Core.Abstractions.Workers;
using Warehouse.Core.Application.EventBus;
using Warehouse.Core.Application.Services;
using Warehouse.Core.Application.Workers;
using Warehouse.Core.Infrastructure;

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
        services.AddInMemoryEventBus();

        services.AddScoped<WebSocketClient>();
        services.AddScoped<ICredentialsProvider, DatabaseCredentialsProvider>();
        services.AddScoped<ICandlestickService, CandlestickService>();
        services.AddSingleton<IWorkerManager, WorkerManager>();
        services.AddSingleton<IMarketDataCache, MarketDataCache>();
        return services;
    }
}
