using System.ComponentModel.DataAnnotations;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Endpoints.Validation;

namespace Warehouse.Backend.Core.Models;

public abstract class BaseMarketDetailsDto
{
    [Required(ErrorMessage = "Market type is required")]
    [ValidEnum(typeof(MarketType), ErrorMessage = "Invalid market type")]
    public MarketType Type { get; init; }
}

public class MarketDetailsDto : BaseMarketDetailsDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ID must be greater than 0")]
    public int Id { get; init; }
}

public class CreateMarketDetailsDto : BaseMarketDetailsDto;

public class UpdateMarketDetailsDto : BaseMarketDetailsDto;

public static class MarketDetailsMappingExtensions
{
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
        };
}
