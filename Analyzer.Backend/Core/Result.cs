namespace Analyzer.Backend.Core;

public readonly struct Result<T>
{
    private readonly T? value;
    private readonly Error? error;
    private readonly bool isSuccess;

    private Result(T value)
    {
        this.value = value;
        error = null;
        isSuccess = true;
    }

    private Result(Error error)
    {
        value = default;
        this.error = error;
        isSuccess = false;
    }

    public bool IsSuccess => isSuccess;

    public T Value => isSuccess ? value! : throw new InvalidOperationException($"Cannot access Value when Result is failure. Error: {error}");

    public Error Error => !isSuccess ? error! : throw new InvalidOperationException("Cannot access Error when Result is success.");

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    public static Result<T> Failure(string code, string message) => new(new Error(code, message));
}

public record Error(string Code, string Message)
{
    public override string ToString() => $"[{Code}] {Message}";
}
