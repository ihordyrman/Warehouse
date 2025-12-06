using Warehouse.Core.Shared.Domain;

namespace Warehouse.Core.Pipelines.Domain;

/// <summary>
///     Represents a single step within a pipeline configuration.
///     Each step references a step definition (via StepTypeKey) and stores
///     user-configured parameter values.
/// </summary>
public class PipelineStep : AuditEntity
{
    /// <summary>
    ///     Unique identifier for this step instance.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    ///     Foreign key to the parent Pipeline.
    /// </summary>
    public int PipelineDetailsId { get; set; }

    /// <summary>
    ///     Navigation property to the parent Pipeline.
    /// </summary>
    public Pipeline Pipeline { get; set; } = null!;

    /// <summary>
    ///     Key identifying the step definition type (e.g., "take-profit", "stop-loss").
    ///     This is used by the StepRegistry to create the actual step instance at runtime.
    /// </summary>
    public string StepTypeKey { get; set; } = string.Empty;

    /// <summary>
    ///     Display name for this step instance.
    ///     Usually matches the step definition name but can be customized.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Execution order within the pipeline.
    ///     Steps are executed in ascending order (1, 2, 3...).
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    ///     Whether this step is enabled.
    ///     Disabled steps are skipped during pipeline execution.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    ///     User-configured parameter values for this step instance.
    ///     Keys map to parameter definitions from the step's schema.
    ///     Values are stored as strings and converted at runtime.
    ///     Example: { "profitPercent": "5.0", "useTrailing": "false" }
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = [];
}
