using System.ComponentModel.DataAnnotations;
using Warehouse.Core.Pipelines.Domain;

namespace Warehouse.Backend.Endpoints.Models;

public abstract class BasePipelineStepModel
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Order is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Order must be a non-negative number")]
    public int Order { get; set; }

    public bool IsEnabled { get; set; }

    public Dictionary<string, string> Parameters { get; set; } = [];
}

public class PipelineStepResponse : BasePipelineStepModel
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ID must be greater than 0")]
    public int Id { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Worker ID must be greater than 0")]
    public int WorkerDetailsId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class CreatePipelineStepRequest : BasePipelineStepModel;

public class UpdatePipelineStepRequest
{
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
    public string? Name { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Order must be a non-negative number")]
    public int? Order { get; set; }

    public bool? IsEnabled { get; set; }

    public Dictionary<string, string>? Parameters { get; set; }
}

public class ReorderPipelineStepsRequest
{
    [Required(ErrorMessage = "Step orders are required")]
    [MinLength(1, ErrorMessage = "At least one step order must be provided")]
    public List<StepOrder> StepOrders { get; set; } = new();

    public class StepOrder
    {
        [Required(ErrorMessage = "Step ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Step ID must be greater than 0")]
        public int StepId { get; set; }

        [Required(ErrorMessage = "Order is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Order must be a non-negative number")]
        public int Order { get; set; }
    }
}

public static class PipelineStepMappingExtensions
{
    public static PipelineStepResponse AsDto(this PipelineStep entity)
        => new()
        {
            Id = entity.Id,
            WorkerDetailsId = entity.WorkerDetailsId,
            Name = entity.Name,
            Order = entity.Order,
            IsEnabled = entity.IsEnabled,
            Parameters = entity.Parameters,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

    public static PipelineStep AsEntity(this CreatePipelineStepRequest request, int workerDetailsId)
        => new()
        {
            WorkerDetailsId = workerDetailsId,
            Name = request.Name,
            Order = request.Order,
            IsEnabled = request.IsEnabled,
            Parameters = request.Parameters
        };

    public static void UpdateFrom(this PipelineStep entity, UpdatePipelineStepRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            entity.Name = request.Name;
        }

        if (request.Order.HasValue)
        {
            entity.Order = request.Order.Value;
        }

        if (request.IsEnabled.HasValue)
        {
            entity.IsEnabled = request.IsEnabled.Value;
        }

        if (request.Parameters != null)
        {
            entity.Parameters = request.Parameters;
        }

        entity.UpdatedAt = DateTime.UtcNow;
    }
}
