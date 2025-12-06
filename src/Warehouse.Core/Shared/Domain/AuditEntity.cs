namespace Warehouse.Core.Shared.Domain;

/// <summary>
///     Base class for entities that track creation and modification timestamps.
///     Timestamps are automatically managed by the DbContext.
/// </summary>
public abstract class AuditEntity
{
    /// <summary>
    ///     When this entity was first created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    ///     When this entity was last modified.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
