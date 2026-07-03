namespace RpFlo.Domain.Common;

public sealed record Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)
    {
        _value = value;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        _error = error;
        IsSuccess = false;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed result.");

    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful result.");

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);

    public Result<TNext> Bind<TNext>(Func<T, Result<TNext>> next) =>
        IsSuccess ? next(Value) : Result<TNext>.Failure(Error);

    public Result<TNext> Map<TNext>(Func<T, TNext> map) =>
        IsSuccess ? Result<TNext>.Success(map(Value)) : Result<TNext>.Failure(Error);

    public T Match(Func<T, T> onSuccess, Func<Error, T> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error);
}
