namespace Warehouse.Backend.Core.Entities;

public class MarketCredentials : AuditEntity
{
    public int Id { get; init; }

    public Market Exchange { get; init; }

    public string ApiKey { get; set; } = string.Empty;

    public string Passphrase { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public bool IsDemo { get; set; }
}
