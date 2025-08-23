using System.Text.Json.Serialization;

namespace Analyzer.Backend.Okx.Messages;

[JsonConverter(typeof(JsonStringEnumConverter<OkxEvent>))]
public enum OkxEvent
{
    Login,
    Subscribe,
    Unsubscribe,
    Order,
    Trade,
    Balance,
    Position,
    Error
}
