using Warehouse.Backend.Core.Domain;

namespace Warehouse.Backend.Core.Models;

using System.ComponentModel.DataAnnotations;

public class CreateWorkerDetailsDto
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

public class UpdateWorkerDetailsDto
{
    public bool? Enabled { get; set; }

    [EnumDataType(typeof(MarketType), ErrorMessage = "Invalid market type")]
    public MarketType? Type { get; set; }

    [StringLength(20, MinimumLength = 3, ErrorMessage = "Symbol must be between 3 and 20 characters")]
    [RegularExpression(@"^[A-Z0-9-/]+$", ErrorMessage = "Symbol must contain only uppercase letters, numbers, hyphens, and slashes")]
    public string? Symbol { get; set; }
}

public class WorkerDetailsDto
{
    public int Id { get; set; }

    public bool Enabled { get; set; }

    public MarketType Type { get; set; }

    public string Symbol { get; set; } = string.Empty;
}

public static class WorkerDetailsMappingExtensions
{
    public static WorkerDetailsDto AsDto(this WorkerDetails entity)
        => new()
        {
            Id = entity.Id,
            Enabled = entity.Enabled,
            Type = entity.Type,
            Symbol = entity.Symbol
        };

    public static WorkerDetails AsEntity(this CreateWorkerDetailsDto dto)
        => new()
        {
            Enabled = dto.Enabled,
            Type = dto.Type,
            Symbol = dto.Symbol.ToUpperInvariant()
        };

    public static void UpdateFrom(this WorkerDetails entity, UpdateWorkerDetailsDto dto)
    {
        if (dto.Enabled.HasValue)
        {
            entity.Enabled = dto.Enabled.Value;
        }

        if (dto.Type.HasValue)
        {
            entity.Type = dto.Type.Value;
        }

        if (!string.IsNullOrWhiteSpace(dto.Symbol))
        {
            entity.Symbol = dto.Symbol.ToUpperInvariant();
        }

        entity.UpdatedAt = DateTime.UtcNow;
    }
}
