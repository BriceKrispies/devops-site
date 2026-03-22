namespace DevOpsSite.Application.Results;

/// <summary>
/// Typed result representing success or failure. Use cases return this, never thrown exceptions.
/// Constitution §9.1: No opaque exception-driven business flow.
/// </summary>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly AppError? _error;

    private Result(T value)
    {
        IsSuccess = true;
        _value = value;
        _error = null;
    }

    private Result(AppError error)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed result.");

    public AppError Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful result.");

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(AppError error) => new(error);

    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsSuccess ? Result<TOut>.Success(map(_value!)) : Result<TOut>.Failure(_error!);

    public async Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> map) =>
        IsSuccess ? Result<TOut>.Success(await map(_value!)) : Result<TOut>.Failure(_error!);
}
