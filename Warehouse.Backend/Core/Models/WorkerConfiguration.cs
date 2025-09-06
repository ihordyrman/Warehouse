using Warehouse.Backend.Core.Domain;

namespace Warehouse.Backend.Core.Models;

public class WorkerConfiguration
{
    public required int WorkerId { get; init; }

    public required bool Enabled { get; init; }

    public required MarketType Type { get; init; }

    public required string Symbol { get; init; }
}
