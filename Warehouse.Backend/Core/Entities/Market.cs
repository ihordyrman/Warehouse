namespace Warehouse.Backend.Core.Entities;

public class Market : AuditEntity
{
    public int? Id { get; init; }

    public MarketType Type { get; init; }

    public string ApiKey { get; set; } = string.Empty;

    public string Passphrase { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public bool IsDemo { get; set; }
}

public abstract class BaseMarketDto
{
    public MarketType Type { get; init; }

    public string ApiKey { get; set; } = string.Empty;

    public string Passphrase { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public bool IsDemo { get; set; }
}

public class MarketDto : BaseMarketDto
{
    public int Id { get; init; }
}

public class CreateMarketDto : BaseMarketDto;

public static class MarketMappingExtensions
{
    public static Market AsEntity(this MarketDto dto)
        => new()
        {
            Id = dto.Id,
            Type = dto.Type,
            ApiKey = dto.ApiKey,
            Passphrase = dto.Passphrase,
            SecretKey = dto.SecretKey,
            IsDemo = dto.IsDemo
        };

    public static Market AsEntity(this CreateMarketDto dto)
        => new()
        {
            Type = dto.Type,
            ApiKey = dto.ApiKey,
            Passphrase = dto.Passphrase,
            SecretKey = dto.SecretKey,
            IsDemo = dto.IsDemo
        };

    public static MarketDto AsDto(this Market market)
        => new()
        {
            Id = market.Id!.Value,
            Type = market.Type,
            ApiKey = market.ApiKey,
            Passphrase = market.Passphrase,
            SecretKey = market.SecretKey,
            IsDemo = market.IsDemo
        };
}
