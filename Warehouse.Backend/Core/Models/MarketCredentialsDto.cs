using System.ComponentModel.DataAnnotations;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Endpoints.Validation;

namespace Warehouse.Backend.Core.Models;

public abstract class BaseMarketCredentialsDto
{
    [Required(ErrorMessage = "Market ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Market ID must be greater than 0")]
    public int? MarketId { get; set; }

    [Required(ErrorMessage = "API Key is required")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "API Key must be between 1 and 500 characters")]
    public string? ApiKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Passphrase is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Passphrase must be between 1 and 200 characters")]
    public string? Passphrase { get; set; } = string.Empty;

    [Required(ErrorMessage = "Secret Key is required")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "Secret Key must be between 1 and 1000 characters")]
    public string? SecretKey { get; set; } = string.Empty;

    public bool IsDemo { get; set; }
}

public class MarketCredentialsDto : BaseMarketCredentialsDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ID must be greater than 0")]
    public int? Id { get; init; }

    [Required(ErrorMessage = "Market type is required")]
    [ValidEnum(typeof(MarketType), ErrorMessage = "Invalid market type")]
    public MarketType? Type { get; init; }
}

public class CreateMarketCredentialsDto : BaseMarketCredentialsDto;

public class UpdateMarketCredentialsDto : BaseMarketCredentialsDto;

public static class MarketMappingExtensions
{
    public static MarketCredentials AsEntity(this CreateMarketCredentialsDto credentialsDto, int marketId)
        => new()
        {
            MarketId = marketId,
            ApiKey = credentialsDto.ApiKey!,
            Passphrase = credentialsDto.Passphrase!,
            SecretKey = credentialsDto.SecretKey!,
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
