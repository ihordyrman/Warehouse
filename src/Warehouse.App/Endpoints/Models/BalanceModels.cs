using Warehouse.Core.Markets.Models;

namespace Warehouse.App.Endpoints.Models;

public class BalanceResponse
{
    public string Currency { get; init; } = string.Empty;

    public decimal Available { get; init; }

    public decimal Total { get; init; }

    public decimal Frozen { get; init; }

    public decimal InOrder { get; init; }

    public string MarketType { get; init; } = string.Empty;

    public DateTime UpdatedAt { get; init; }

    public static BalanceResponse FromDomain(Balance balance)
        => new()
        {
            Currency = balance.Currency,
            Available = balance.Available,
            Total = balance.Total,
            Frozen = balance.Frozen,
            InOrder = balance.InOrder,
            MarketType = balance.MarketType.ToString(),
            UpdatedAt = balance.UpdatedAt
        };
}

public class AccountBalanceResponse
{
    public string MarketType { get; init; } = string.Empty;

    public decimal TotalEquity { get; init; }

    public decimal AvailableBalance { get; init; }

    public decimal UsedMargin { get; init; }

    public decimal UnrealizedPnl { get; init; }

    public List<BalanceResponse> Balances { get; init; } = [];

    public DateTime UpdatedAt { get; init; }

    public static AccountBalanceResponse FromDomain(AccountBalance account)
        => new()
        {
            MarketType = account.MarketType.ToString(),
            TotalEquity = account.TotalEquity,
            AvailableBalance = account.AvailableBalance,
            UsedMargin = account.UsedMargin,
            UnrealizedPnl = account.UnrealizedPnl,
            Balances = account.Balances.Select(BalanceResponse.FromDomain).ToList(),
            UpdatedAt = account.UpdatedAt
        };
}

public class BalanceSnapshotResponse
{
    public string MarketType { get; init; } = string.Empty;

    public Dictionary<string, BalanceResponse> Spot { get; init; } = [];

    public Dictionary<string, BalanceResponse> Funding { get; init; } = [];

    public AccountBalanceResponse? AccountSummary { get; init; }

    public DateTime Timestamp { get; init; }

    public static BalanceSnapshotResponse FromDomain(BalanceSnapshot snapshot)
        => new()
        {
            MarketType = snapshot.MarketType.ToString(),
            Spot = snapshot.Spot.ToDictionary(kvp => kvp.Key, kvp => BalanceResponse.FromDomain(kvp.Value)),
            Funding = snapshot.Funding.ToDictionary(kvp => kvp.Key, kvp => BalanceResponse.FromDomain(kvp.Value)),
            AccountSummary = snapshot.AccountSummary != null ? AccountBalanceResponse.FromDomain(snapshot.AccountSummary) : null,
            Timestamp = snapshot.Timestamp
        };
}
