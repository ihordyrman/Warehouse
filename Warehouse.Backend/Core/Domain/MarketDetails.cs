namespace Warehouse.Backend.Core.Domain;

public class MarketDetails : AuditEntity
{
    public int Id { get; init; }

    public MarketType Type { get; init; }

    public MarketCredentials? Credentials { get; set; }
}

public abstract class BaseMarketDetailsDto
{
    public MarketType Type { get; set; }
}

public class MarketDetailsDto : BaseMarketDetailsDto
{
    public int Id { get; init; }

    public MarketCredentialsDto? Credentials { get; init; }
}

public class MarketDetailsWithCredentialsDto : BaseMarketDetailsDto
{
    public int Id { get; init; }

    public MarketCredentialsDto? Credentials { get; set; }
}

public class CreateMarketDetailsDto : BaseMarketDetailsDto;

public static class MarketDetailsMappingExtensions
{
    public static MarketDetails AsEntity(this MarketDetailsDto detailsDto)
        => new()
        {
            Id = detailsDto.Id,
            Type = detailsDto.Type
        };

    public static MarketDetails AsEntity(this CreateMarketDetailsDto detailsDto)
        => new()
        {
            Type = detailsDto.Type
        };

    public static MarketDetailsDto AsDto(this MarketDetails marketDetails)
        => new()
        {
            Id = marketDetails.Id,
            Type = marketDetails.Type,
            Credentials = marketDetails.Credentials?.AsDto()
        };

    public static MarketDetailsWithCredentialsDto AsDtoWithCredentials(this MarketDetails marketDetails)
        => new()
        {
            Id = marketDetails.Id,
            Type = marketDetails.Type,
            Credentials = marketDetails.Credentials?.AsDto()
        };
}
