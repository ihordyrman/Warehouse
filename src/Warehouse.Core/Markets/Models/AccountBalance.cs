using Warehouse.Core.Markets.Domain;

namespace Warehouse.Core.Markets.Models;

public class AccountBalance
{
    public MarketType MarketType { get; init; }

    public decimal TotalEquity { get; init; }

    public decimal AvailableBalance { get; init; }

    public decimal UsedMargin { get; init; }

    public decimal UnrealizedPnl { get; init; }

    public List<Balance> Balances { get; init; } = [];

    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
