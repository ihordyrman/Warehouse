using System.Text.Json.Serialization;

namespace Warehouse.Backend.Markets.Okx.Messages;

[JsonConverter(typeof(JsonStringEnumConverter<OkxResponseCode>))]
public enum OkxResponseCode
{
    Success = 0,
    InvalidTimestamp = 60004,
    InvalidApiKey = 60005,
    TimestampExpired = 60006,
    InvalidSignature = 60007,
    NoSubscriptionChannels = 60008,
    LoginFailed = 60009,
    PleaseLogin = 60011,
    InvalidRequest = 60012,
    InvalidArguments = 60013,
    RequestsTooFrequent = 60014,
    WrongUrlOrChannel = 60018,
    InvalidOperation = 60019,
    MultipleLoginsNotAllowed = 60030,
    InternalLoginError = 63999,
    RateLimitReached = 50011,
    SystemBusy = 50026
}
