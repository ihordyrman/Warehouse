using Warehouse.Core.Markets.Domain;

namespace Warehouse.Core.Markets.Models;

public sealed record MarketDataEvent(string Symbol, MarketType Source, long SequenceNumber, string[][] Asks, string[][] Bids);
