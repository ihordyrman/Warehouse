using Microsoft.Extensions.Logging;
using Warehouse.Core.Markets.Concrete.Okx.Messages.Http;
using Warehouse.Core.Markets.Contracts;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Markets.Models;
using Warehouse.Core.Shared;

namespace Warehouse.Core.Markets.Concrete.Okx.Services;

public class OkxBalanceProvider(OkxHttpService okxHttpService, ILogger<OkxBalanceProvider> logger) : IMarketBalanceProvider
{
    public MarketType MarketType => MarketType.Okx;

    public async Task<Result<BalanceSnapshot>> GetBalancesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = new BalanceSnapshot
            {
                MarketType = MarketType.Okx,
                Timestamp = DateTime.UtcNow
            };

            Result<OkxFundingBalance[]> fundingResult = await okxHttpService.GetFundingBalanceAsync();
            if (fundingResult is { IsSuccess: true, Value: not null })
            {
                foreach (OkxFundingBalance funding in fundingResult.Value)
                {
                    Balance balance = MapFundingBalance(funding);
                    snapshot.Funding[balance.Currency] = balance;
                }
            }

            Result<OkxAccountBalance[]> accountResult = await okxHttpService.GetAccountBalanceAsync();
            if (accountResult is { IsSuccess: true, Value.Length: > 0 })
            {
                OkxAccountBalance okxAccount = accountResult.Value[0];
                snapshot.AccountSummary = MapAccountBalance(okxAccount);

                foreach (OkxBalanceDetail detail in okxAccount.Details)
                {
                    Balance balance = MapBalanceDetail(detail);
                    snapshot.Spot[balance.Currency] = balance;
                }
            }

            Result<OkxBalanceDetail[]> assetResult = await okxHttpService.GetBalanceAsync();
            if (assetResult is { IsSuccess: true, Value: not null })
            {
                foreach (OkxBalanceDetail asset in assetResult.Value)
                {
                    Balance balance = MapAssetBalance(asset);
                    snapshot.Spot.TryAdd(balance.Currency, balance);
                }
            }

            logger.LogInformation(
                "Successfully retrieved balance snapshot for OKX with {SpotCount} spot and {FundingCount} funding balances",
                snapshot.Spot.Count,
                snapshot.Funding.Count);

            return Result<BalanceSnapshot>.Success(snapshot);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get balance snapshot from OKX");
            return Result<BalanceSnapshot>.Failure(new Error($"Failed to get balances: {ex.Message}"));
        }
    }

    public async Task<Result<Balance>> GetBalanceAsync(string currency, CancellationToken cancellationToken = default)
    {
        try
        {
            Result<OkxBalanceDetail[]> result = await okxHttpService.GetBalanceAsync(currency);

            if (!result.IsSuccess)
            {
                return Result<Balance>.Failure(result.Error);
            }

            OkxBalanceDetail? okxBalance = result.Value.FirstOrDefault(x => x.Ccy.Equals(currency, StringComparison.OrdinalIgnoreCase));

            if (okxBalance is null)
            {
                return Result<Balance>.Failure(new Error($"Currency {currency} not found"));
            }

            Balance balance = MapAssetBalance(okxBalance);
            return Result<Balance>.Success(balance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get balance for currency {Currency} from OKX", currency);
            return Result<Balance>.Failure(new Error($"Failed to get balance: {ex.Message}"));
        }
    }

    public async Task<Result<decimal>> GetTotalUsdtValueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Result<OkxAssetsValuation[]> portfolioResult = await okxHttpService.GetAssetsValuationAsync();

            if (portfolioResult.IsSuccess)
            {
                logger.LogInformation("Successfully retrieved total portfolio value: {Value} USDT", portfolioResult.Value);
                return Result<decimal>.Success(portfolioResult.Value.Sum(x => ParseDecimal(x.TotalBalance)));
            }

            logger.LogWarning("Failed to get total portfolio value from OKX: {Error}", portfolioResult.Error.Message);
            return Result<decimal>.Failure(new Error("Failed to get total USDT value"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get total USDT value");
            return Result<decimal>.Failure(new Error($"Failed to get total USDT value: {ex.Message}"));
        }
    }

    private static Balance MapFundingBalance(OkxFundingBalance funding)
        => new()
        {
            Currency = funding.Ccy,
            Available = ParseDecimal(funding.AvailBal),
            Total = ParseDecimal(funding.Bal),
            Frozen = ParseDecimal(funding.FrozenBal),
            InOrder = 0,
            MarketType = MarketType.Okx
        };

    private static Balance MapBalanceDetail(OkxBalanceDetail detail)
        => new()
        {
            Currency = detail.Ccy,
            Available = ParseDecimal(detail.AvailBal),
            Total = ParseDecimal(detail.CashBal),
            Frozen = ParseDecimal(detail.FrozenBal),
            InOrder = ParseDecimal(detail.OrdFrozen),
            MarketType = MarketType.Okx
        };

    private static Balance MapAssetBalance(OkxBalanceDetail asset)
        => new()
        {
            Currency = asset.Ccy,
            Available = ParseDecimal(asset.AvailBal),
            Total = ParseDecimal(asset.Eq),
            Frozen = ParseDecimal(asset.FrozenBal),
            InOrder = ParseDecimal(asset.OrdFrozen),
            MarketType = MarketType.Okx
        };

    private AccountBalance MapAccountBalance(OkxAccountBalance okxAccount)
    {
        var accountBalance = new AccountBalance
        {
            MarketType = MarketType.Okx,
            TotalEquity = ParseDecimal(okxAccount.TotalEq),
            AvailableBalance = ParseDecimal(okxAccount.AvailEq),
            UsedMargin = ParseDecimal(okxAccount.Imr),
            UnrealizedPnl = ParseDecimal(okxAccount.Upl),
            UpdatedAt = DateTime.UtcNow
        };

        foreach (OkxBalanceDetail detail in okxAccount.Details)
        {
            accountBalance.Balances.Add(MapBalanceDetail(detail));
        }

        return accountBalance;
    }

    private static decimal ParseDecimal(string value) => decimal.TryParse(value, out decimal result) ? result : 0m;
}
