using System.Text.Json.Serialization;
using Warehouse.Core.Markets.Concrete.Okx.Messages.Http;
using Warehouse.Core.Markets.Concrete.Okx.Messages.Socket;

namespace Warehouse.Core.Markets.Concrete.Okx;

[JsonSerializable(typeof(OkxSocketArgs))]
[JsonSerializable(typeof(OkxSocketSubscriptionData))]
[JsonSerializable(typeof(OkxSocketResponse))]
[JsonSerializable(typeof(OkxSocketEventResponse))]
[JsonSerializable(typeof(OkxSocketLoginResponse))]
[JsonSerializable(typeof(OkxSocketBookData))]
[JsonSerializable(typeof(OkxHttpResponse<OkxAccountBalance[]>))]
[JsonSerializable(typeof(OkxHttpResponse<OkxBalanceDetail[]>))]
[JsonSerializable(typeof(OkxHttpResponse<OkxFundingBalance[]>))]
[JsonSerializable(typeof(OkxHttpResponse<OkxOrder[]>))]
[JsonSerializable(typeof(OkxHttpResponse<OkxOrderBook[]>))]
[JsonSerializable(typeof(OkxHttpResponse<OkxTicker[]>))]
[JsonSerializable(typeof(OkxHttpResponse<OkxCandlestick[]>))]
[JsonSerializable(typeof(OkxAccountBalance))]
[JsonSerializable(typeof(OkxBalanceDetail))]
[JsonSerializable(typeof(OkxFundingBalance))]
[JsonSerializable(typeof(OkxOrder))]
[JsonSerializable(typeof(OkxOrderBook))]
[JsonSerializable(typeof(OkxTicker))]
internal partial class OkxJsonContext : JsonSerializerContext;
