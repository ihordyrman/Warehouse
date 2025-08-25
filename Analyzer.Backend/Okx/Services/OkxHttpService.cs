using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Analyzer.Backend.Okx.Configurations;
using Analyzer.Backend.Okx.Messages.Http;
using Microsoft.Extensions.Options;

namespace Analyzer.Backend.Okx.Services;

public class OkxHttpService(
    IOptions<OkxAuthConfiguration> okxAuthConfiguration,
    ILogger<OkxHttpService> logger,
    IHttpClientFactory httpClientFactory) : IDisposable
{
    private readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions
    {
        TypeInfoResolver = OkxJsonContext.Default
    };

    public async Task SendMessageAsync()
    {
        HttpClient httpClient = httpClientFactory.CreateClient("Okx");
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        httpClient.DefaultRequestHeaders.Add("OK-ACCESS-TIMESTAMP", timestamp);
        httpClient.DefaultRequestHeaders.Add(
            "OK-ACCESS-SIGN",
            OkxAuthService.GenerateSignature(timestamp, okxAuthConfiguration.Value.SecretKey, "GET", "/api/v5/account/balance"));

        HttpResponseMessage httpResponseMessage = await httpClient.GetAsync("/api/v5/account/balance");

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            OkxHttpResponse<OkxAccountBalance>? response = await httpResponseMessage.Content.ReadFromJsonAsync<OkxHttpResponse<OkxAccountBalance>>(serializerOptions);
        }
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}
