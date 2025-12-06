using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Core.Infrastructure.Common;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Infrastructure.WebSockets;
using Warehouse.Core.Markets.Concrete.Okx.Services;
using Warehouse.Core.Markets.Contracts;
using Warehouse.Core.Markets.Services;
using Warehouse.Core.Orders.Contracts;
using Warehouse.Core.Orders.Services;
using Warehouse.Core.Pipelines.Core;
using Warehouse.Core.Shared.Services;

namespace Warehouse.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataProtection(x => x.ApplicationDiscriminator = "Warehouse")
            .PersistKeysToFileSystem(new DirectoryInfo(Directory.GetCurrentDirectory()))
            .SetApplicationName("Warehouse.App")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(365));

        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));

        services.AddDbContext<WarehouseDbContext>((sp, options) =>
        {
            DatabaseSettings settings = configuration.GetSection(DatabaseSettings.SectionName).Get<DatabaseSettings>() ??
                                        throw new InvalidOperationException(
                                            $"'{DatabaseSettings.SectionName}' configuration section is missing.");
            options.UseNpgsql(settings.ConnectionString);
        });

        services.AddScoped<WebSocketClient>();
        services.AddScoped<ICredentialsProvider, DatabaseCredentialsProvider>();
        services.AddScoped<ICandlestickService, CandlestickService>();
        services.AddScoped<IBalanceManager, BalanceManager>();
        services.AddScoped<IOrderManager, OrderManager>();
        services.AddSingleton<IMarketDataCache, MarketDataCache>();
        services.AddSingleton<IPipelineOrchestrator, PipelineOrchestrator>();
        services.AddHostedService(sp => (PipelineOrchestrator)sp.GetRequiredService<IPipelineOrchestrator>());

        return services;
    }
}
