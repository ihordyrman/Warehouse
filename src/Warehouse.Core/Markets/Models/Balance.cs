using Warehouse.Core.Markets.Domain;

namespace Warehouse.Core.Markets.Models;

public class Balance
{
    public string Currency { get; init; } = string.Empty;

    public decimal Available { get; init; }

    public decimal Total { get; init; }

    public decimal Frozen { get; init; }

    public decimal InOrder { get; init; }

    public MarketType MarketType { get; init; }

    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
