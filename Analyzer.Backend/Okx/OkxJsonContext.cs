using System.Text.Json.Serialization;
using Analyzer.Backend.Okx.Messages;
using Analyzer.Backend.Okx.Messages.Http;
using Analyzer.Backend.Okx.Messages.Socket;

namespace Analyzer.Backend.Okx;

[JsonSerializable(typeof(OkxSocketArgs))]
[JsonSerializable(typeof(OkxSocketSubscriptionData))]
[JsonSerializable(typeof(OkxSocketResponse))]
[JsonSerializable(typeof(OkxSocketEventResponse))]
[JsonSerializable(typeof(OkxSocketLoginResponse))]
[JsonSerializable(typeof(OkxSocketBookData))]
[JsonSerializable(typeof(OkxHttpResponse<OkxAccountBalance>))]
[JsonSerializable(typeof(OkxAccountBalance))]
[JsonSerializable(typeof(OkxBalanceDetail))]
[JsonSerializable(typeof(OkxFundingBalance))]
internal partial class OkxJsonContext : JsonSerializerContext;
