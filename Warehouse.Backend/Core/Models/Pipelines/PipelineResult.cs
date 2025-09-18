namespace Warehouse.Backend.Core.Models.Pipelines;

public class PipelineResult
{
    public bool IsSuccess { get; init; }

    public string? Message { get; init; }

    public Exception? Exception { get; init; }

    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;

    public TimeSpan Duration { get; init; }

    public Dictionary<string, object> OutputData { get; init; } = [];

    public static PipelineResult Success(string? message = null)
        => new()
        {
            IsSuccess = true,
            Message = message
        };

    public static PipelineResult Failure(string message, Exception? exception = null)
        => new()
        {
            IsSuccess = false,
            Message = message,
            Exception = exception
        };
}
