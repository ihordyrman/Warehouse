// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Shared.Domain;

namespace Warehouse.Core.Workers.Domain;

public class WorkerDetails : AuditEntity
{
    public int Id { get; init; }

    public bool Enabled { get; set; }

    public MarketType Type { get; set; }

    public string Symbol { get; set; } = string.Empty;
}
