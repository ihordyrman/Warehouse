using Warehouse.Core.Domain;
using Warehouse.Core.Infrastructure;
using Warehouse.Core.Pipelines.Base;

namespace Warehouse.Core.Pipelines;

public class TradingPipelineContext : IPipelineContext
{
    public required string Symbol { get; init; }

    public required MarketType MarketType { get; init; }

    public required MarketData MarketData { get; init; }

    public int WorkerId { get; init; }

    public decimal MaxPositionSize { get; init; }

    public decimal MaxLossPerTrade { get; init; }

    public decimal CurrentBalance { get; init; }

    public TradingSignal? Signal { get; set; }

    public OrderRequest? OrderRequest { get; set; }

    public Guid ExecutionId { get; } = Guid.CreateVersion7();

    public DateTime StartedAt { get; } = DateTime.UtcNow;

    public bool IsCancelled { get; set; }
}
