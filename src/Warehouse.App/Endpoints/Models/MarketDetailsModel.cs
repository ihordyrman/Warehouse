using System.ComponentModel.DataAnnotations;
using Warehouse.App.Endpoints.Validation;
using Warehouse.Core.Markets.Domain;

namespace Warehouse.App.Endpoints.Models;

public abstract class BaseMarketModel
{
    [Required(ErrorMessage = "Market type is required")]
    [ValidEnum(typeof(MarketType), ErrorMessage = "Invalid market type")]
    public MarketType Type { get; init; }
}

public class MarketResponse : BaseMarketModel
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ID must be greater than 0")]
    public int Id { get; init; }
}

public class CreateMarketRequest : BaseMarketModel;

public class UpdateMarketRequest : BaseMarketModel;

public static class MarketMappingExtensions
{
    public static MarketDetails AsEntity(this CreateMarketRequest request)
        => new()
        {
            Type = request.Type
        };

    public static MarketResponse AsDto(this MarketDetails marketDetails)
        => new()
        {
            Id = marketDetails.Id,
            Type = marketDetails.Type
        };
}
