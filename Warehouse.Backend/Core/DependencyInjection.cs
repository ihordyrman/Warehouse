using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Warehouse.Backend.Core.Abstractions.Markets;
using Warehouse.Backend.Core.Abstractions.Workers;
using Warehouse.Backend.Core.Application.EventBus;
using Warehouse.Backend.Core.Application.Workers;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Infrastructure;

namespace Warehouse.Backend.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreDependencies(this IServiceCollection services)
    {
        services.AddDataProtection(x => x.ApplicationDiscriminator = "Warehouse")
            .PersistKeysToFileSystem(new DirectoryInfo(Directory.GetCurrentDirectory()))
            .SetApplicationName("Warehouse.Backend")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(365));

        // todo: while in local development
        services.AddDbContext<WarehouseDbContext>(options => options.UseNpgsql("Host=localhost;Database=warehouse;Username=postgres;Password=postgres"));
        services.AddInMemoryEventBus();

        services.AddScoped<WebSocketClient>();
        services.AddScoped<ICredentialsProvider, DatabaseCredentialsProvider>();
        services.AddSingleton<IWorkerManager, WorkerManager>();
        services.AddSingleton<IMarketDataCache, MarketDataCache>();
        services.AddHostedService<WorkerOrchestrator>();
        return services;
    }

    public static async Task<IApplicationBuilder> EnsureDbReadinessAsync(this WebApplication app)
    {
        IServiceScopeFactory? scopeFactory = app.Services.GetService<IServiceScopeFactory>();
        using IServiceScope scope = scopeFactory!.CreateScope();
        WarehouseDbContext? dbContext = scope.ServiceProvider.GetService<WarehouseDbContext>();

        IConfiguration configuration = scope.ServiceProvider.GetService<IConfiguration>()!;

        await dbContext!.Database.EnsureCreatedAsync();
        await EnsureCredentialsPopulated(configuration, dbContext);

        return app;
    }

    private static async Task EnsureCredentialsPopulated(IConfiguration configuration, WarehouseDbContext dbContext)
    {
        if (await dbContext.MarketCredentials.AnyAsync())
        {
            return;
        }

        const string section = "OkxAuthConfiguration";
        string apiKey = configuration[$"{section}:ApiKey"] ?? throw new ArgumentNullException();
        string passPhrase = configuration[$"{section}:Passphrase"] ?? throw new ArgumentNullException();
        string secretKey = configuration[$"{section}:SecretKey"] ?? throw new ArgumentNullException();

        MarketDetails? market = await dbContext.MarketDetails.FirstOrDefaultAsync(x => x.Type == MarketType.Okx);
        if (market is null)
        {
            market = new MarketDetails
            {
                Type = MarketType.Okx
            };

            dbContext.MarketDetails.Add(market);
        }

        var marketCredentials = new MarketCredentials
        {
            ApiKey = apiKey,
            Passphrase = passPhrase,
            SecretKey = secretKey,
            MarketDetails = market
        };

        dbContext.MarketCredentials.Add(marketCredentials);
        await dbContext.SaveChangesAsync();
    }
}
