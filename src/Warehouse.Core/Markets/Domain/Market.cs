using Warehouse.Core.Shared.Domain;

namespace Warehouse.Core.Markets.Domain;

public class Market : AuditEntity
{
    public int Id { get; init; }

    public MarketType Type { get; init; }

    public MarketCredentials? Credentials { get; init; }
}
