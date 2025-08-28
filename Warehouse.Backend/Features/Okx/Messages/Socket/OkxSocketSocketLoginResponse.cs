using System.Text.Json.Serialization;

namespace Warehouse.Backend.Features.Okx.Messages.Socket;

public record OkxSocketLoginResponse : OkxSocketEventResponse
{
    [JsonPropertyName("msg")]
    public string? Message { get; init; }

    [JsonPropertyName("code")]
    public OkxResponseCode? Code { get; init; }

    [JsonPropertyName("connId")]
    public string? ConnectionId { get; init; }
}
