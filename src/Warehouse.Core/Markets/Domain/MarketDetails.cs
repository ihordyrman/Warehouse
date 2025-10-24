using Warehouse.Core.Shared.Domain;

namespace Warehouse.Core.Markets.Domain;

public class MarketDetails : AuditEntity
{
    public int Id { get; init; }

    public MarketType Type { get; init; }

    public MarketAccount? Credentials { get; init; }
}
