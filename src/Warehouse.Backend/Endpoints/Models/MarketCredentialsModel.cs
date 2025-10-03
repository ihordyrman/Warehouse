using System.ComponentModel.DataAnnotations;
using Warehouse.Core.Domain;

namespace Warehouse.Backend.Endpoints.Models;

public abstract class BaseMarketCredentialsModel
{
    [Required(ErrorMessage = "Market ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Market ID must be greater than 0")]
    public int? MarketId { get; set; }

    [Required(ErrorMessage = "API Key is required")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "API Key must be between 1 and 500 characters")]
    public string? ApiKey { get; init; } = string.Empty;

    [Required(ErrorMessage = "Passphrase is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Passphrase must be between 1 and 200 characters")]
    public string? Passphrase { get; init; } = string.Empty;

    [Required(ErrorMessage = "Secret Key is required")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "Secret Key must be between 1 and 1000 characters")]
    public string? SecretKey { get; init; } = string.Empty;
}

public class MarketCredentialsResponse : BaseMarketCredentialsModel
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ID must be greater than 0")]
    public int? Id { get; init; }
}

public class CreateMarketCredentialsRequest : BaseMarketCredentialsModel;

public class UpdateMarketCredentialsRequest : BaseMarketCredentialsModel;

public static class MarketCredentialsMappingExtensions
{
    public static MarketCredentials AsEntity(this CreateMarketCredentialsRequest credentialsResponse, int marketId)
        => new()
        {
            MarketId = marketId,
            ApiKey = credentialsResponse.ApiKey!,
            Passphrase = credentialsResponse.Passphrase!,
            SecretKey = credentialsResponse.SecretKey!
        };

    public static MarketCredentialsResponse AsDto(this MarketCredentials marketCredentials)
        => new()
        {
            Id = marketCredentials.Id,
            MarketId = marketCredentials.MarketId,
            ApiKey = marketCredentials.ApiKey,
            Passphrase = marketCredentials.Passphrase,
            SecretKey = marketCredentials.SecretKey
        };
}
