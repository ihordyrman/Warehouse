using System.Text.Json.Serialization;
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
[JsonSerializable(typeof(OkxHttpResponse<OkxBalanceDetail>))]
[JsonSerializable(typeof(OkxHttpResponse<List<OkxFundingBalance>>))]
[JsonSerializable(typeof(OkxHttpResponse<List<OkxOrder>>))]
[JsonSerializable(typeof(OkxHttpResponse<List<OkxTicker>>))]
[JsonSerializable(typeof(OkxAccountBalance))]
[JsonSerializable(typeof(OkxBalanceDetail))]
[JsonSerializable(typeof(OkxFundingBalance))]
[JsonSerializable(typeof(OkxOrder))]
[JsonSerializable(typeof(OkxTicker))]
internal partial class OkxJsonContext : JsonSerializerContext;
