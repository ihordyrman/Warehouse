// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Shared.Domain;

namespace Warehouse.Core.Pipelines.Domain;

/// <summary>
///     Represents a trading pipeline configuration.
///     A pipeline defines an automated trading strategy for a specific symbol/market combination.
/// </summary>
public class Pipeline : AuditEntity
{
    /// <summary>
    ///     Unique identifier for this pipeline.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     User-friendly name for this pipeline (e.g., "BTC Scalping Strategy").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     The trading pair symbol (e.g., "BTC-USDT", "ETH-USDT").
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    ///     The market/exchange this pipeline trades on (e.g., OKX Spot, OKX Futures).
    /// </summary>
    public MarketType MarketType { get; set; }

    /// <summary>
    ///     Whether this pipeline is enabled and should run.
    ///     Disabled pipelines are skipped by the orchestrator.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    ///     How often the pipeline executes its steps.
    /// </summary>
    public TimeSpan ExecutionInterval { get; set; }

    /// <summary>
    ///     When this pipeline last completed an execution cycle.
    ///     Used for monitoring and debugging.
    /// </summary>
    public DateTime? LastExecutedAt { get; set; }

    /// <summary>
    ///     Current operational status of the pipeline.
    /// </summary>
    public PipelineStatus Status { get; set; }

    /// <summary>
    ///     Ordered list of steps that execute when the pipeline runs.
    ///     Steps are executed in Order ascending.
    /// </summary>
    public List<PipelineStep> Steps { get; set; } = [];

    /// <summary>
    ///     User-defined tags for organizing and filtering pipelines.
    /// </summary>
    public List<string> Tags { get; set; } = [];
}

/// <summary>
///     Operational status of a pipeline.
/// </summary>
public enum PipelineStatus
{
    /// <summary>Pipeline is configured but not currently running.</summary>
    Idle,

    /// <summary>Pipeline is actively executing.</summary>
    Running,

    /// <summary>Pipeline is temporarily paused by user.</summary>
    Paused,

    /// <summary>Pipeline encountered an error and stopped.</summary>
    Error
}
