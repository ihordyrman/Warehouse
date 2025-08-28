using System.Text.Json.Serialization;

namespace Warehouse.Backend.Features.Okx.Messages;

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
