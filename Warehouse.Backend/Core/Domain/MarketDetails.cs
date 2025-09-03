namespace Warehouse.Backend.Core.Domain;

public class MarketDetails : AuditEntity
{
    public int Id { get; init; }

    public MarketType Type { get; init; }

    public MarketCredentials? Credentials { get; set; }
}
