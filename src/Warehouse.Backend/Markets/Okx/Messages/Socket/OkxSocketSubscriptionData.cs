using System.Text.Json.Serialization;

namespace Warehouse.Backend.Markets.Okx.Messages.Socket;

public record OkxSocketSubscriptionData
{
    [JsonPropertyName("asks")]
    public List<List<string>> Asks { get; init; } = [];

    [JsonPropertyName("bids")]
    public List<List<string>> Bids { get; init; } = [];

    [JsonPropertyName("ts")]
    public string Timestamp { get; init; } = string.Empty;

    [JsonPropertyName("checksum")]
    public long Checksum { get; init; }

    [JsonPropertyName("seqId")]
    public long SequenceId { get; init; }

    [JsonPropertyName("prevSeqId")]
    public long PreviousSequenceId { get; init; }
}
