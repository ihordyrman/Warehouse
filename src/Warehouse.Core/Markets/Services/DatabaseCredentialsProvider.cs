using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Contracts;
using Warehouse.Core.Markets.Domain;

namespace Warehouse.Core.Markets.Services;

/// <summary>
///     Retrieves market credentials from the database.
///     Implements the ICredentialsProvider interface using EF Core.
/// </summary>
public class DatabaseCredentialsProvider(IServiceScopeFactory scopeFactory, ILogger<DatabaseCredentialsProvider> logger)
    : ICredentialsProvider
{
    /// <inheritdoc />
    public async Task<MarketCredentials> GetCredentialsAsync(MarketType marketType, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        WarehouseDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

        MarketCredentials? credentials = await dbContext.MarketCredentials.Include(x => x.Market)
            .FirstOrDefaultAsync(x => x.Market.Type == marketType, cancellationToken);

        if (credentials is null)
        {
            throw new InvalidOperationException($"No credentials found for {marketType}");
        }

        logger.LogDebug("Retrieved credentials for {Market}", marketType);
        return credentials;
    }
}
