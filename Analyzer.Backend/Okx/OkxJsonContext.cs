using System.Text.Json.Serialization;
using Analyzer.Backend.Okx.Messages;

namespace Analyzer.Backend.Okx;

[JsonSerializable(typeof(OkxSocketArgs))]
[JsonSerializable(typeof(OkxSocketSubscriptionData))]
[JsonSerializable(typeof(OkxSocketResponse))]
[JsonSerializable(typeof(OkxSocketEventResponse))]
[JsonSerializable(typeof(OkxSocketLoginResponse))]
[JsonSerializable(typeof(OkxSocketBookData))]
internal partial class OkxJsonContext : JsonSerializerContext;
