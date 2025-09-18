using System.ComponentModel.DataAnnotations;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Endpoints.Validation;

namespace Warehouse.Backend.Core.Models.Endpoints;

public abstract class BaseMarketDto
{
    [Required(ErrorMessage = "Market type is required")]
    [ValidEnum(typeof(MarketType), ErrorMessage = "Invalid market type")]
    public MarketType Type { get; init; }
}

public class MarketDto : BaseMarketDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ID must be greater than 0")]
    public int Id { get; init; }
}

public class CreateMarketDto : BaseMarketDto;

public class UpdateMarketDto : BaseMarketDto;

public static class MarketMappingExtensions
{
    public static MarketDetails AsEntity(this CreateMarketDto dto)
        => new()
        {
            Type = dto.Type
        };

    public static MarketDto AsDto(this MarketDetails marketDetails)
        => new()
        {
            Id = marketDetails.Id,
            Type = marketDetails.Type
        };
}
