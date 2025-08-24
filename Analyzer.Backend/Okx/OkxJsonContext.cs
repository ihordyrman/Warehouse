using System.Text.Json.Serialization;
using Analyzer.Backend.Okx.Messages;

namespace Analyzer.Backend.Okx;

[JsonSerializable(typeof(OkxSocketSubscriptionArgs))]
[JsonSerializable(typeof(OkxSocketSubscriptionData))]
[JsonSerializable(typeof(OkxSocketSubscriptionResponse))]
[JsonSerializable(typeof(OkxSocketEventResponse))]
[JsonSerializable(typeof(OkxSocketLoginResponse))]
internal partial class OkxJsonContext : JsonSerializerContext;
