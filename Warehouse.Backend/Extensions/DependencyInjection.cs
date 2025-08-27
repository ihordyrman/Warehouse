using System.Threading.Channels;
using Polly;
using Warehouse.Backend.Core;
using Warehouse.Backend.Okx.Configurations;
using Warehouse.Backend.Okx.Constants;
using Warehouse.Backend.Okx.Handlers;
using Warehouse.Backend.Okx.Models;
using Warehouse.Backend.Okx.Processors;
using Warehouse.Backend.Okx.Services;

namespace Warehouse.Backend.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddOkxSupport(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OkxAuthConfiguration>(configuration.GetSection(nameof(OkxAuthConfiguration)));
        services.AddSingleton<IWebSocketClient, WebSocketClient>();
        services.AddSingleton<OkxHeartbeatService>();
        services.AddSingleton<OkxWebSocketService>();
        services.AddSingleton<OkxHttpService>();
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

        OkxAuthConfiguration config = configuration.GetSection(nameof(OkxAuthConfiguration)).Get<OkxAuthConfiguration>()!;
        services.AddHttpClient(
                "Okx",
                client =>
                {
                    client.BaseAddress = new Uri(config.Url);
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.Add("User-Agent", "Analyzer/1.0");
                    client.DefaultRequestHeaders.Add("OK-ACCESS-KEY", config.ApiKey);
                    client.DefaultRequestHeaders.Add("OK-ACCESS-PASSPHRASE", config.Passphrase);
                })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
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
