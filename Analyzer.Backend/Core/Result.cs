using System.Net;

namespace Analyzer.Backend.Core;

public readonly struct Result<T>
{
    private readonly T? value;
    private readonly Error? error;

    private Result(T value)
    {
        this.value = value;
        error = null;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        value = default;
        this.error = error;
        IsSuccess = false;
    }

    public bool IsSuccess { get; }

    public T Value
        => IsSuccess ? value! : throw new InvalidOperationException($"Cannot access Value when Result is failure. Error: {error}");

    public Error Error => !IsSuccess ? error! : throw new InvalidOperationException("Cannot access Error when Result is success.");

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    public static Result<T> Failure(string message, HttpStatusCode code) => new(new Error(message, code));

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
        => IsSuccess ? onSuccess(value!) : onFailure(error!);

    public Result<T> OnFailure(Action<Error> action)
    {
        if (!IsSuccess)
        {
            action(error!);
        }

        return this;
    }

    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess)
        {
            action(value!);
        }

        return this;
    }
}

public record Error(string Message, HttpStatusCode? Code = null)
{
    public override string ToString() => Code is not null ? $"[{Code}] {Message}" : $"{Message}";
}
