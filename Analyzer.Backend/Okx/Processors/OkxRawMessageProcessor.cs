using System.Threading.Channels;

namespace Analyzer.Backend.Okx.Processors;

public class OkxRawMessageProcessor(
    [FromKeyedServices("RawMessages")] Channel<string> messageChannel,
    ILogger<OkxRawMessageProcessor> logger)
{
    private readonly ChannelReader<string> messageReader = messageChannel.Reader;

    public async Task StartProcessingAsync(CancellationToken cancellationToken)
    {
        await foreach (string rawMessage in messageReader.ReadAllAsync(cancellationToken))
        {
            try
            {
                logger.LogInformation("Received message: {Message}", rawMessage);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message");
            }
        }
    }
}
