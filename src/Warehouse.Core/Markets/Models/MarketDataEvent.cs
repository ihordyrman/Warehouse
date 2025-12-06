using Warehouse.Core.Markets.Domain;

namespace Warehouse.Core.Markets.Models;

/// <summary>
///     Event representing an update to the order book.
/// </summary>
public sealed record MarketDataEvent(string Symbol, MarketType Source, string[][] Asks, string[][] Bids);
