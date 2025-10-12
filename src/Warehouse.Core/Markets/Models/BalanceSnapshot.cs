using Warehouse.Core.Markets.Domain;

namespace Warehouse.Core.Markets.Models;

public class BalanceSnapshot
{
    public MarketType MarketType { get; init; }

    public Dictionary<string, Balance> Spot { get; init; } = [];

    public Dictionary<string, Balance> Funding { get; init; } = [];

    public AccountBalance? AccountSummary { get; set; }

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
