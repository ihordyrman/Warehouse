﻿namespace Warehouse.Core.Models.Pipelines;

public class PipelineStepResult
{
    public bool ShouldContinue { get; init; } = true;

    public bool IsSuccess { get; init; } = true;

    public string? Message { get; init; }

    public Dictionary<string, object> Data { get; init; } = [];

    public static PipelineStepResult Continue(string? message = null)
        => new()
        {
            ShouldContinue = true,
            IsSuccess = true,
            Message = message
        };

    public static PipelineStepResult Stop(string? message = null)
        => new()
        {
            ShouldContinue = false,
            IsSuccess = true,
            Message = message
        };

    public static PipelineStepResult Error(string message)
        => new()
        {
            ShouldContinue = false,
            IsSuccess = false,
            Message = message
        };
}
