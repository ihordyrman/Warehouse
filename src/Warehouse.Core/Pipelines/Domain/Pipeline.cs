// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Shared.Domain;

namespace Warehouse.Core.Pipelines.Domain;

public class Pipeline : AuditEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public MarketType MarketType { get; set; }

    public bool Enabled { get; set; }

    public TimeSpan ExecutionInterval { get; set; }

    public DateTime? LastExecutedAt { get; set; }

    public PipelineStatus Status { get; set; }

    public List<PipelineStep> Steps { get; set; } = [];

    public List<string> Tags { get; set; } = [];
}

public enum PipelineStatus
{
    Idle,
    Running,
    Paused,
    Error
}
