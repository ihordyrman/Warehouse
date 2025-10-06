namespace Warehouse.Core.Shared.Domain;

public abstract class AuditEntity
{
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
