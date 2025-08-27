namespace Warehouse.Backend.Core.Entities;

public abstract class AuditEntity
{
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
