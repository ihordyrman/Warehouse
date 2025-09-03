namespace Warehouse.Backend.Core.Domain;

public class MarketCredentials : AuditEntity
{
    public int Id { get; init; }

    public int MarketId { get; init; }

    public MarketDetails MarketDetails { get; init; } = null!;

    public string ApiKey { get; set; } = string.Empty;

    public string Passphrase { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public bool IsDemo { get; set; }
}
