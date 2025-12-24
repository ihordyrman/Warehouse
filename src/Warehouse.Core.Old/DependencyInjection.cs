using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Core.Old.Functional.Infrastructure.WebSockets;
using Warehouse.Core.Old.Functional.Markets.Contracts;
using Warehouse.Core.Old.Functional.Markets.Services;
using Warehouse.Core.Old.Functional.Orders.Contracts;
using Warehouse.Core.Old.Infrastructure.Common;
using Warehouse.Core.Old.Markets.Services;
using Warehouse.Core.Old.Orders.Services;
using Warehouse.Core.Old.Pipelines.Builder;
using Warehouse.Core.Old.Pipelines.Core;
using Warehouse.Core.Old.Pipelines.Registry;
using Warehouse.Core.Old.Shared.Services;

namespace Warehouse.Core.Old;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataProtection(x => x.ApplicationDiscriminator = "Warehouse")
            .PersistKeysToFileSystem(new DirectoryInfo(Directory.GetCurrentDirectory()))
            .SetApplicationName("Warehouse.App")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(365));

        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
        services.AddScoped<WebSocketClient>();
        services.AddScoped<ICredentialsProvider, DatabaseCredentialsProvider>();
        services.AddScoped<ICandlestickService, CandlestickService>();
        services.AddScoped<IBalanceManager, BalanceManager>();
        services.AddScoped<IOrderManager, OrderManager>();
        services.AddSingleton<IMarketDataCache, MarketDataCache>();
        services.AddSingleton<IPipelineOrchestrator, PipelineOrchestrator>();
        services.AddSingleton<IStepRegistry, StepRegistry>();
        services.AddScoped<IPipelineBuilder, PipelineBuilder>();
        services.AddSingleton<IPipelineExecutorFactory, PipelineExecutorFactory>();
        services.AddHostedService(x => (PipelineOrchestrator)x.GetRequiredService<IPipelineOrchestrator>());
        services.AddHostedService<MarketConnectionService>();

        return services;
    }
}
