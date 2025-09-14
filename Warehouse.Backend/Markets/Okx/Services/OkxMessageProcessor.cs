using System.Text.Json;
using Warehouse.Backend.Core.Abstractions.Markets;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Infrastructure;
using Warehouse.Backend.Core.Models;
using Warehouse.Backend.Markets.Okx.Messages;
using Warehouse.Backend.Markets.Okx.Messages.Socket;

namespace Warehouse.Backend.Markets.Okx.Services;

internal sealed class OkxMessageProcessor(ILogger<OkxMessageProcessor> logger, IMarketDataCache marketDataCache)
{
    private readonly JsonSerializerOptions serializerOptions = new()
    {
        TypeInfoResolver = OkxJsonContext.Default
    };

    private long sequenceNumber;

    public async Task ProcessMessageAsync(WebSocketMessage message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null)
        {
            return;
        }

        try
        {
            if (message.Text == "pong")
            {
                logger.LogTrace("Heartbeat pong received");
                return;
            }

            OkxSocketResponse? okxMessage = JsonSerializer.Deserialize<OkxSocketResponse>(message.Text, serializerOptions);
            if (okxMessage == null)
            {
                logger.LogWarning("Failed to deserialize message: {Message}", message.Text);
                return;
            }

            await RouteMessageAsync(okxMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message: {Message}", message.Text);
        }
    }

    private async Task RouteMessageAsync(OkxSocketResponse message, CancellationToken cancellationToken)
    {
        if (message.Event == OkxEvent.Subscribe)
        {
            logger.LogInformation(
                "Subscription confirmed: {Channel}:{Symbol}",
                message.Arguments?.Channel,
                message.Arguments?.InstrumentId);
            return;
        }

        if (message.Event == OkxEvent.Error)
        {
            logger.LogError("OKX Error - Code: {Code}, Message: {Message}", message.Code, message.Message);
            return;
        }

        if (message.Data?.Length > 0 && message.Arguments?.InstrumentId != null)
        {
            await ProcessMarketDataAsync(message, cancellationToken);
        }
    }

    private async Task ProcessMarketDataAsync(OkxSocketResponse message, CancellationToken cancellationToken)
    {
        string symbol = message.Arguments!.InstrumentId!;
        OkxSocketBookData data = message.Data![0];

        if (data.Asks == null || data.Bids == null)
        {
            logger.LogWarning("Invalid market data for {Symbol}: missing asks or bids", symbol);
            return;
        }

        marketDataCache.Update(
            new MarketDataEvent(symbol, MarketType.Okx, Interlocked.Increment(ref sequenceNumber), data.Asks, data.Bids));
    }
}
