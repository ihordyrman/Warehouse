﻿using System.Text.Json.Serialization;

namespace Warehouse.Backend.Markets.Okx.Messages.Socket;

public record OkxSocketBookData
{
    [JsonPropertyName("asks")]
    public string[][]? Asks { get; init; }

    [JsonPropertyName("bids")]
    public string[][]? Bids { get; init; }

    [JsonPropertyName("ts")]
    public string? Timestamp { get; init; }

    [JsonPropertyName("checksum")]
    public long? Checksum { get; init; }

    [JsonPropertyName("seqId")]
    public long SequenceId { get; init; }

    [JsonPropertyName("prevSeqId")]
    public long PreviousSequenceId { get; init; }
}
