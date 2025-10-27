using Warehouse.Core.Markets.Domain;

namespace Warehouse.Core.Markets.Models;

public sealed record MarketDataEvent(string Symbol, MarketType Source, string[][] Asks, string[][] Bids);
