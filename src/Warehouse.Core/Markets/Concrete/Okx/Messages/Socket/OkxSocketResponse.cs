using System.Text.Json.Serialization;

namespace Warehouse.Core.Markets.Concrete.Okx.Messages.Socket;

public record OkxSocketResponse : OkxSocketEventResponse
{
    [JsonPropertyName("arg")]
    public OkxSocketArgs? Arguments { get; init; }

    [JsonPropertyName("action")]
    public OkxAction? Action { get; init; }

    [JsonPropertyName("data")]
    public OkxSocketBookData[]? Data { get; init; }

    [JsonPropertyName("msg")]
    public string? Message { get; set; }

    [JsonPropertyName("code")]
    public OkxResponseCode? Code { get; set; }
}
