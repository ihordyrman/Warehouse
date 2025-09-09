using System.Net;
using System.Threading.Channels;
using Polly;
using Warehouse.Backend.Core;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Models;
using Warehouse.Backend.Markets.Okx.Constants;
using Warehouse.Backend.Markets.Okx.Handlers;
using Warehouse.Backend.Markets.Okx.Services;

namespace Warehouse.Backend.Markets.Okx;

public static class DependencyInjection
{
    public static IServiceCollection AddOkxSupport(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MarketCredentials>(configuration.GetSection(nameof(MarketCredentials)));
        services.AddSingleton<IWebSocketClient, WebSocketClient>();
        services.AddSingleton<OkxHeartbeatService>();
        services.AddSingleton<OkxWebSocketService>();
        services.AddSingleton<OkxHttpService>();
        services.AddSingleton<OkxMarketAdapter>();

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

        services.AddHttpClient(
                "Okx",
                client =>
                {
                    client.BaseAddress = Urls.GetMarketUrl(MarketType.Okx);
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.Add("User-Agent", "Analyzer/1.0");
                })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })
            .AddPolicyHandler((provider, _) =>
            {
                ILogger<HttpClient> logger = provider.GetRequiredService<ILogger<HttpClient>>();
                return Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .WaitAndRetryAsync(
                        3,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        (outcome, timespan, retryCount, context) =>
                        {
                            logger.LogWarning("Retry attempt {RetryCount} after {TimeSpan} seconds", retryCount, timespan);
                        });
            })
            .AddPolicyHandler(_ => Policy.TimeoutAsync<HttpResponseMessage>(10));

        return services;
    }
}
