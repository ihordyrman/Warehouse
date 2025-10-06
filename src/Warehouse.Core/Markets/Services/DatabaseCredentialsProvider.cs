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
    public async Task<MarketCredentials> GetCredentialsAsync(MarketType marketType, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        WarehouseDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

        MarketCredentials? credentials = await dbContext.MarketCredentials.Include(x => x.MarketDetails)
            .FirstOrDefaultAsync(x => x.MarketDetails.Type == marketType, cancellationToken);

        if (credentials == null)
        {
            throw new InvalidOperationException($"No credentials found for {marketType}");
        }

        logger.LogDebug("Retrieved credentials for {Market}", marketType);
        return credentials;
    }

    public async Task<IReadOnlyList<MarketCredentials>> GetAllCredentialsAsync(
        MarketType? marketType = null,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        WarehouseDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

        IQueryable<MarketCredentials> query = dbContext.MarketCredentials.Include(x => x.MarketDetails).AsQueryable();

        if (marketType.HasValue)
        {
            query = query.Where(x => x.MarketDetails.Type == marketType.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
