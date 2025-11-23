using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Markets.Models;
using Warehouse.Core.Pipelines.Core;

namespace Warehouse.Core.Pipelines.Trading;

public class TradingContext : IPipelineContext
{
    public Guid ExecutionId { get; } = Guid.CreateVersion7();

    public DateTime StartedAt { get; } = DateTime.UtcNow;

    public bool IsCancelled { get; set; }

    public required int PipelineId { get; init; }

    public MarketData? CurrentMarketData { get; set; }

    public MarketType MarketType { get; init; }

    public string Symbol { get; init; } = string.Empty;

    public decimal? BuyPrice { get; set; }

    public decimal? Quantity { get; set; }

    public TradingAction Action { get; set; } = TradingAction.None;

    public long? ActiveOrderId { get; set; }

    public decimal CurrentPrice { get; set; }
}
