using System.Text.Json;
using System.Text.Json.Serialization;

namespace Warehouse.Backend.Markets.Okx.Messages.Http;

[JsonConverter(typeof(OkxCandlestickConverter))]
public record OkxCandlestick
{
    public string[] Data { get; set; } = [];

    public DateTime Timestamp => Data?.Length > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(Data[0])).DateTime.ToUniversalTime() : default;

    public decimal Open => Data?.Length > 1 ? decimal.Parse(Data[1]) : 0;

    public decimal High => Data?.Length > 2 ? decimal.Parse(Data[2]) : 0;

    public decimal Low => Data?.Length > 3 ? decimal.Parse(Data[3]) : 0;

    public decimal Close => Data?.Length > 4 ? decimal.Parse(Data[4]) : 0;

    public decimal Volume => Data?.Length > 5 ? decimal.Parse(Data[5]) : 0;

    public decimal VolumeCurrency => Data?.Length > 6 ? decimal.Parse(Data[6]) : 0;

    public decimal VolumeQuoteCurrency => Data?.Length > 7 ? decimal.Parse(Data[7]) : 0;

    public bool IsCompleted => Data?.Length > 8 && Data[8] == "1";
}

public class OkxCandlestickConverter : JsonConverter<OkxCandlestick>
{
    public override OkxCandlestick Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string[] data = JsonSerializer.Deserialize<string[]>(ref reader, options) ??
                        throw new JsonException("Failed to deserialize candlestick data");

        return new OkxCandlestick { Data = data };
    }

    public override void Write(Utf8JsonWriter writer, OkxCandlestick value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value.Data, options);
}
