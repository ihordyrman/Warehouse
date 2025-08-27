using System.Text.Json.Serialization;

namespace Warehouse.Backend.Okx.Messages.Http;

public record OkxHttpResponse<T>
{
    [JsonPropertyName("code")]
    public OkxResponseCode Code { get; init; }

    [JsonPropertyName("msg")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; init; }
}
