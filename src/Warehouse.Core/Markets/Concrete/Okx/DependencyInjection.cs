using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Warehouse.Core.Infrastructure.WebSockets;
using Warehouse.Core.Markets.Concrete.Okx.Services;
using Warehouse.Core.Markets.Contracts;
using Warehouse.Core.Markets.Domain;

namespace Warehouse.Core.Markets.Concrete.Okx;

public static class DependencyInjection
{
    public static IServiceCollection AddOkxSupport(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MarketCredentials>(configuration.GetSection(nameof(MarketCredentials)));
        services.AddSingleton<IWebSocketClient, WebSocketClient>();
        services.AddSingleton<OkxHeartbeatService>();
        services.AddScoped<OkxHttpService>();
        services.AddScoped<IMarketBalanceProvider, OkxBalanceProvider>();
        services.AddSingleton<OkxMarketAdapter>();
        services.AddHostedService<OkxSynchronizationWorker>();

        services.AddHttpClient(
                "Okx",
                client =>
                {
                    client.BaseAddress = new Uri("https://www.okx.com/");
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
                        (_, timespan, retryCount, _) =>
                        {
                            logger.LogWarning("Retry attempt {RetryCount} after {TimeSpan} seconds", retryCount, timespan);
                        });
            })
            .AddPolicyHandler(_ => Policy.TimeoutAsync<HttpResponseMessage>(10));

        return services;
    }
}
