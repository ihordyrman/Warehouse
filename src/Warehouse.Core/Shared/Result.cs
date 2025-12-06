using System.Net;

namespace Warehouse.Core.Shared;

/// <summary>
///     Represents the outcome of an operation, which can be either a success with a value or a failure with an error.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
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

    /// <summary>
    ///     Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     Gets the value if the result is a success. Throws an exception if accessed on failure.
    /// </summary>
    public T Value
        => IsSuccess ? value! : throw new InvalidOperationException($"Cannot access Value when Result is failure. Error: {error}");

    public Error Error => !IsSuccess ? error! : throw new InvalidOperationException("Cannot access Error when Result is success.");

    /// <summary>
    ///     Creates a successful result with the given value.
    /// </summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    ///     Creates a failed result with the given error.
    /// </summary>
    public static Result<T> Failure(Error error) => new(error);

    /// <summary>
    ///     Creates a failed result with a message and HTTP status code.
    /// </summary>
    public static Result<T> Failure(string message, HttpStatusCode code) => new(new Error(message, code));

    /// <summary>
    ///     Executes the appropriate function based on the result state (success or failure).
    /// </summary>
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

/// <summary>
///     Represents an error with a message and an optional HTTP status code.
/// </summary>
public record Error(string Message, HttpStatusCode? Code = null)
{
    public override string ToString() => Code is not null ? $"[{Code}] {Message}" : $"{Message}";
}
