using Warehouse.Core.Shared.Domain;

namespace Warehouse.Core.Pipelines.Domain;

public class PipelineStep : AuditEntity
{
    public int Id { get; init; }

    public int PipelineDetailsId { get; set; }

    public Pipeline Pipeline { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public int Order { get; set; }

    public bool IsEnabled { get; set; }

    public Dictionary<string, string> Parameters { get; set; } = [];
}
