using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Models;

namespace Warehouse.Backend.Core.Abstractions.Markets;

public interface IMarketDataCache
{
    MarketData? GetData(string symbol, MarketType marketType);

    void Update(MarketDataEvent marketDataEvent);
}
