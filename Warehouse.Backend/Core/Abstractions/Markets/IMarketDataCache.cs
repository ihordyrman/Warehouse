using Warehouse.Backend.Core.Models;

namespace Warehouse.Backend.Core.Abstractions.Markets;

public interface IMarketDataCache
{
    MarketData? GetData(string symbol);

    void Update(string symbol, MarketData data);

    IReadOnlySet<string> GetCachedSymbols();
}
