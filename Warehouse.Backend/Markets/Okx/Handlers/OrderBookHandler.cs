﻿using System.Threading.Channels;
using Warehouse.Backend.Core.Models;
using Warehouse.Backend.Markets.Okx.Constants;
using Warehouse.Backend.Markets.Okx.Messages;
using Warehouse.Backend.Markets.Okx.Messages.Socket;

namespace Warehouse.Backend.Markets.Okx.Handlers;

public class OrderBookHandler(
    ILogger<OrderBookHandler> logger,
    [FromKeyedServices(OkxChannelNames.MarketData)] Channel<MarketData> marketDataChannel) : IOkxMessageHandler
{
    private readonly ChannelWriter<MarketData> marketDataWriter = marketDataChannel.Writer;

    public Task<bool> CanHandleAsync(OkxSocketResponse message) => Task.FromResult(message.Arguments?.Channel == "books");

    public async Task HandleAsync(OkxSocketResponse message, CancellationToken cancellationToken)
    {
        try
        {
            if (message.Event == OkxEvent.Subscribe)
            {
                logger.LogInformation(
                    "Successfully subscribed to {Channel}:{Instrument}",
                    message.Arguments!.Channel,
                    message.Arguments.InstrumentId);
                return;
            }

            if (message.Data?.Length > 0)
            {
                var marketData = new MarketData(
                    message.Arguments!.InstrumentId!,
                    message.Data[0].Asks!,
                    message.Data[0].Bids!);
                await marketDataWriter.WriteAsync(marketData, cancellationToken);
                return;
            }

            logger.LogWarning("Unable to process okx socket message: {Message}", message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message");
        }
    }
}
