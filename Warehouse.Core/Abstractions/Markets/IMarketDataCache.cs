using Warehouse.Core.Domain;
using Warehouse.Core.Models;

namespace Warehouse.Core.Abstractions.Markets;

public interface IMarketDataCache
{
    MarketData? GetData(string symbol, MarketType marketType);

    void Update(MarketDataEvent marketDataEvent);
}
