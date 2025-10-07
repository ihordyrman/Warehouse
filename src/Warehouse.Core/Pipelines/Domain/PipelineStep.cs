using Warehouse.Core.Shared.Domain;
using Warehouse.Core.Workers.Domain;

namespace Warehouse.Core.Pipelines.Domain;

public class PipelineStep : AuditEntity
{
    public int Id { get; init; }

    public int WorkerDetailsId { get; set; }

    public WorkerDetails WorkerDetails { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public int Order { get; set; }

    public bool IsEnabled { get; set; }

    public Dictionary<string, string> Parameters { get; set; } = [];
}
