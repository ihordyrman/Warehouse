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
            .PersistKeysToFileSystem(new DirectoryInfo(Paths.GetDataProtectionPath()))
            .SetApplicationName("Warehouse.Backend")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(365));

        string connectionString = $"Data Source={Paths.GetDatabasePath()}";
        services.AddDbContext<WarehouseDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<WebSocketClient>();
        services.AddInMemoryEventBus();

        services.AddHostedService<WorkerOrchestrator>();
        services.AddSingleton<IWorkerManager, WorkerManager>();
        services.AddSingleton<IMarketDataCache, MarketDataCache>();
        services.AddScoped<ICredentialsProvider, DatabaseCredentialsProvider>();

        return services;
    }

    public static async Task<IApplicationBuilder> EnsureDbReadinessAsync(this WebApplication app)
    {
        IServiceScopeFactory? scopeFactory = app.Services.GetService<IServiceScopeFactory>();
        using IServiceScope scope = scopeFactory!.CreateScope();
        WarehouseDbContext? dbContext = scope.ServiceProvider.GetService<WarehouseDbContext>();

        IConfiguration configuration = scope.ServiceProvider.GetService<IConfiguration>()!;

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
