using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Warehouse.Core.Markets.Contracts;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Markets.Models;
using Warehouse.Core.Shared;

namespace Warehouse.Core.Markets.Services;

public class BalanceManager : IBalanceManager
{
    private readonly ILogger<BalanceManager> logger;
    private readonly Dictionary<MarketType, IMarketBalanceProvider> providers = new();

    public BalanceManager(IServiceProvider serviceProvider, ILogger<BalanceManager> logger)
    {
        this.logger = logger;

        IEnumerable<IMarketBalanceProvider> marketProviders = serviceProvider.GetServices<IMarketBalanceProvider>();

        foreach (IMarketBalanceProvider provider in marketProviders)
        {
            providers[provider.MarketType] = provider;
            logger.LogInformation("Registered balance provider for {MarketType}", provider.MarketType);
        }
    }

    public async Task<Result<Balance>> GetBalanceAsync(
        MarketType marketType,
        string currency,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (providers.TryGetValue(marketType, out IMarketBalanceProvider? provider))
            {
                return await provider.GetBalanceAsync(currency, cancellationToken);
            }

            return Result<Balance>.Failure(new Error($"No balance provider registered for {marketType}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get balance for {Currency} on {MarketType}", currency, marketType);
            return Result<Balance>.Failure(new Error($"Failed to get balance: {ex.Message}"));
        }
    }

    public async Task<Result<BalanceSnapshot>> GetAllBalancesAsync(MarketType marketType, CancellationToken cancellationToken = default)
    {
        try
        {
            if (providers.TryGetValue(marketType, out IMarketBalanceProvider? provider))
            {
                return await provider.GetBalancesAsync(cancellationToken);
            }

            return Result<BalanceSnapshot>.Failure(new Error($"No balance provider registered for {marketType}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all balances for {MarketType}", marketType);
            return Result<BalanceSnapshot>.Failure(new Error($"Failed to get balances: {ex.Message}"));
        }
    }

    public async Task<Result<AccountBalance>> GetAccountBalanceAsync(MarketType marketType, CancellationToken cancellationToken = default)
    {
        try
        {
            Result<BalanceSnapshot> snapshotResult = await GetAllBalancesAsync(marketType, cancellationToken);

            if (!snapshotResult.IsSuccess)
            {
                return Result<AccountBalance>.Failure(snapshotResult.Error);
            }

            if (snapshotResult.Value.AccountSummary is null)
            {
                return Result<AccountBalance>.Failure(new Error("Account summary not available"));
            }

            return Result<AccountBalance>.Success(snapshotResult.Value.AccountSummary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get account balance for {MarketType}", marketType);
            return Result<AccountBalance>.Failure(new Error($"Failed to get account balance: {ex.Message}"));
        }
    }

    public async Task<Result<List<Balance>>> GetNonZeroBalancesAsync(MarketType marketType, CancellationToken cancellationToken = default)
    {
        try
        {
            Result<BalanceSnapshot> snapshotResult = await GetAllBalancesAsync(marketType, cancellationToken);

            if (!snapshotResult.IsSuccess)
            {
                return Result<List<Balance>>.Failure(snapshotResult.Error);
            }

            var nonZeroBalances = new List<Balance>();

            nonZeroBalances.AddRange(snapshotResult.Value.Spot.Values.Where(x => x.Total > 0 || x.Available > 0 || x.Frozen > 0));
            nonZeroBalances.AddRange(snapshotResult.Value.Funding.Values.Where(x => x.Total > 0 || x.Available > 0 || x.Frozen > 0));

            logger.LogInformation("Found {Count} non-zero balances for {MarketType}", nonZeroBalances.Count, marketType);

            return Result<List<Balance>>.Success(nonZeroBalances);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get non-zero balances for {MarketType}", marketType);
            return Result<List<Balance>>.Failure(new Error($"Failed to get non-zero balances: {ex.Message}"));
        }
    }

    public async Task<Result<decimal>> GetTotalUsdtValueAsync(MarketType marketType, CancellationToken cancellationToken = default)
    {
        try
        {
            if (providers.TryGetValue(marketType, out IMarketBalanceProvider? provider))
            {
                return await provider.GetTotalUsdtValueAsync(cancellationToken);
            }

            return Result<decimal>.Failure(new Error($"No balance provider registered for {marketType}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get total USDT value for {MarketType}", marketType);
            return Result<decimal>.Failure(new Error($"Failed to get total USDT value: {ex.Message}"));
        }
    }
}
