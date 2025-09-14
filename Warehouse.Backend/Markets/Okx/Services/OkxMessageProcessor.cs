using System.Text.Json;
using System.Threading.Channels;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Infrastructure;
using Warehouse.Backend.Core.Models;
using Warehouse.Backend.Markets.Okx.Messages;
using Warehouse.Backend.Markets.Okx.Messages.Socket;

namespace Warehouse.Backend.Markets.Okx.Services;

internal sealed class OkxMessageProcessor(Channel<MarketDataEvent> marketDataChannel, ILogger<OkxMessageProcessor> logger) : IDisposable
{
    private readonly JsonSerializerOptions serializerOptions = new()
    {
        TypeInfoResolver = OkxJsonContext.Default
    };
    private bool disposed;
    private long sequenceNumber;

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        marketDataChannel.Writer.TryComplete();
    }

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

        var marketDataEvent = new MarketDataEvent
        {
            Symbol = symbol,
            Data = new MarketData(symbol, data.Asks, data.Bids),
            ReceivedAt = DateTime.UtcNow,
            Source = MarketType.Okx,
            SequenceNumber = Interlocked.Increment(ref sequenceNumber)
        };

        if (!marketDataChannel.Writer.TryWrite(marketDataEvent))
        {
            logger.LogWarning("Market data channel is full, dropping message for {Symbol}", symbol);
        }
        else
        {
            logger.LogTrace("Market data processed for {Symbol}, seq: {Seq}", symbol, marketDataEvent.SequenceNumber);
        }
    }
}

public sealed class MarketDataEvent
{
    public required string Symbol { get; init; }

    public required MarketData Data { get; init; }

    public required DateTime ReceivedAt { get; init; }

    public required MarketType Source { get; init; }

    public long SequenceNumber { get; init; }
}
