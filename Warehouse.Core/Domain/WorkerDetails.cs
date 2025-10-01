// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace Warehouse.Core.Domain;

public class WorkerDetails : AuditEntity
{
    public int Id { get; init; }

    public bool Enabled { get; set; }

    public MarketType Type { get; set; }

    public string Symbol { get; set; } = string.Empty;
}
