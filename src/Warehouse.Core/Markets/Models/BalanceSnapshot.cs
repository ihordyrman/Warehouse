using Warehouse.Core.Markets.Domain;

namespace Warehouse.Core.Markets.Models;

public class BalanceSnapshot
{
    public MarketType MarketType { get; init; }

    public Dictionary<string, Balance> Spot { get; } = [];

    public Dictionary<string, Balance> Funding { get; } = [];

    public AccountBalance? AccountSummary { get; set; }

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
