using Warehouse.Core.Shared.Domain;

namespace Warehouse.Core.Markets.Domain;

public class MarketCredentials : AuditEntity
{
    public int Id { get; init; }

    public int MarketId { get; init; }

    public Market Market { get; init; } = null!;

    public string ApiKey { get; set; } = string.Empty;

    public string Passphrase { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public bool IsSandbox { get; set; } = false;
}
