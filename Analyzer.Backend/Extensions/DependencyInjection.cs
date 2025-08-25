using System.Threading.Channels;
using Analyzer.Backend.Core;
using Analyzer.Backend.Okx.Configurations;
using Analyzer.Backend.Okx.Constants;
using Analyzer.Backend.Okx.Handlers;
using Analyzer.Backend.Okx.Models;
using Analyzer.Backend.Okx.Processors;
using Analyzer.Backend.Okx.Services;

namespace Analyzer.Backend.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddOkxSupport(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OkxAuthConfiguration>(configuration.GetSection(nameof(OkxAuthConfiguration)));
        services.AddSingleton<IWebSocketClient, WebSocketClient>();
        services.AddSingleton<OkxHeartbeatService>();
        services.AddSingleton<OkxWebSocketService>();
        services.AddHostedService<OkxMarketDataProcessor>();
        services.AddHostedService<OkxMessageProcessor>();

        services.AddKeyedSingleton(
            OkxChannelNames.MarketData,
            (_, _) =>
            {
                var options = new BoundedChannelOptions(1000)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = true
                };
                return Channel.CreateBounded<MarketData>(options);
            });

        services.AddSingleton<IOkxMessageHandler, OrderBookHandler>();

        return services;
    }
}
