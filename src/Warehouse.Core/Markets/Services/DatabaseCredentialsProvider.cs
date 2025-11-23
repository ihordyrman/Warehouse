using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Contracts;
using Warehouse.Core.Markets.Domain;

namespace Warehouse.Core.Markets.Services;

public class DatabaseCredentialsProvider(IServiceScopeFactory scopeFactory, ILogger<DatabaseCredentialsProvider> logger)
    : ICredentialsProvider
{
    public async Task<MarketAccount> GetCredentialsAsync(MarketType marketType, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        WarehouseDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

        MarketAccount? credentials = await dbContext.MarketAccounts.Include(x => x.MarketDetails)
            .FirstOrDefaultAsync(x => x.MarketDetails.Type == marketType, cancellationToken);

        if (credentials is null)
        {
            throw new InvalidOperationException($"No credentials found for {marketType}");
        }

        logger.LogDebug("Retrieved credentials for {Market}", marketType);
        return credentials;
    }
}
