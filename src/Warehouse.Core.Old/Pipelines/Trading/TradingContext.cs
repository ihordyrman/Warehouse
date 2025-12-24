using Warehouse.Core.Old.Functional.Markets.Contracts;
using Warehouse.Core.Old.Functional.Markets.Domain;
using Warehouse.Core.Old.Pipelines.Core;

namespace Warehouse.Core.Old.Pipelines.Trading;

public class TradingContext : IPipelineContext
{
    public required int PipelineId { get; init; }

    public MarketData? CurrentMarketData { get; set; }

    public MarketType MarketType { get; init; }

    public required string Symbol { get; init; }

    public decimal? BuyPrice { get; set; }

    public decimal? Quantity { get; set; }

    public TradingAction Action { get; set; } = TradingAction.None;

    public long? ActiveOrderId { get; set; }

    public decimal CurrentPrice { get; set; }

    public Guid ExecutionId { get; } = Guid.CreateVersion7();

    public DateTime StartedAt { get; } = DateTime.UtcNow;

    public bool IsCancelled { get; set; }
}
