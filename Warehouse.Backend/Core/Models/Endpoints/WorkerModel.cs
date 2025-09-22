using System.ComponentModel.DataAnnotations;
using Warehouse.Backend.Core.Domain;

namespace Warehouse.Backend.Core.Models.Endpoints;

public class CreateWorkerRequest
{
    public bool Enabled { get; set; } = false;

    [Required(ErrorMessage = "Market type is required")]
    [EnumDataType(typeof(MarketType), ErrorMessage = "Invalid market type")]
    public MarketType Type { get; set; }

    [Required(ErrorMessage = "Symbol is required")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Symbol must be between 3 and 20 characters")]
    [RegularExpression(@"^[A-Z0-9-/]+$", ErrorMessage = "Symbol must contain only uppercase letters, numbers, hyphens, and slashes")]
    public string Symbol { get; set; } = string.Empty;
}

public class UpdateWorkerRequest
{
    public bool? Enabled { get; set; }

    [EnumDataType(typeof(MarketType), ErrorMessage = "Invalid market type")]
    public MarketType? Type { get; set; }

    [StringLength(20, MinimumLength = 3, ErrorMessage = "Symbol must be between 3 and 20 characters")]
    [RegularExpression(@"^[A-Z0-9-/]+$", ErrorMessage = "Symbol must contain only uppercase letters, numbers, hyphens, and slashes")]
    public string? Symbol { get; set; }
}

public class WorkerResponse
{
    public int Id { get; set; }

    public bool Enabled { get; set; }

    public MarketType Type { get; set; }

    public string Symbol { get; set; } = string.Empty;
}

public static class WorkerMappingExtensions
{
    public static WorkerResponse AsDto(this WorkerDetails entity)
        => new()
        {
            Id = entity.Id,
            Enabled = entity.Enabled,
            Type = entity.Type,
            Symbol = entity.Symbol
        };

    public static WorkerDetails AsEntity(this CreateWorkerRequest request)
        => new()
        {
            Enabled = request.Enabled,
            Type = request.Type,
            Symbol = request.Symbol.ToUpperInvariant()
        };

    public static void UpdateFrom(this WorkerDetails entity, UpdateWorkerRequest request)
    {
        if (request.Enabled.HasValue)
        {
            entity.Enabled = request.Enabled.Value;
        }

        if (request.Type.HasValue)
        {
            entity.Type = request.Type.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Symbol))
        {
            entity.Symbol = request.Symbol.ToUpperInvariant();
        }

        entity.UpdatedAt = DateTime.UtcNow;
    }
}
