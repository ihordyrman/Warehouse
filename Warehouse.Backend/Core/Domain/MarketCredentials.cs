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

public abstract class BaseMarketCredentialsDto
{
    public string ApiKey { get; set; } = string.Empty;

    public string Passphrase { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public bool IsDemo { get; set; }
}

public class MarketCredentialsDto : BaseMarketCredentialsDto
{
    public int Id { get; init; }

    public int MarketId { get; set; }

    public MarketType Type { get; init; }
}

public class CreateMarketCredentialsDto : BaseMarketCredentialsDto;

public class UpdateMarketCredentialsDto : BaseMarketCredentialsDto;

public static class MarketMappingExtensions
{
    public static MarketCredentials AsEntity(this CreateMarketCredentialsDto credentialsDto, int marketId)
        => new()
        {
            MarketId = marketId,
            ApiKey = credentialsDto.ApiKey,
            Passphrase = credentialsDto.Passphrase,
            SecretKey = credentialsDto.SecretKey,
            IsDemo = credentialsDto.IsDemo
        };

    public static MarketCredentialsDto AsDto(this MarketCredentials marketCredentials)
        => new()
        {
            Id = marketCredentials.Id,
            MarketId = marketCredentials.MarketId,
            Type = marketCredentials.MarketDetails.Type,
            ApiKey = marketCredentials.ApiKey,
            Passphrase = marketCredentials.Passphrase,
            SecretKey = marketCredentials.SecretKey,
            IsDemo = marketCredentials.IsDemo
        };
}
