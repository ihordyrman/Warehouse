using System.Text.Json.Serialization;

namespace Warehouse.Core.Markets.Concrete.Okx.Messages.Socket;

public record OkxSocketArgs
{
    [JsonPropertyName("channel")]
    public string? Channel { get; init; }

    [JsonPropertyName("instId")]
    public string? InstrumentId { get; set; }
}
