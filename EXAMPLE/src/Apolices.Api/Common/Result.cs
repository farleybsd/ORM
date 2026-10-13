namespace Apolices.Api.Common;

public class Result
{
    public bool IsSuccess { get; }
    public Erro? Error { get; }

    protected Result(bool isSuccess, Erro? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsFailure => !IsSuccess;

    public static Result Success() => new(true, null);
    public static Result Failure(Erro error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Failure<T>(Erro error) => new(default!, false, error);
}

public class Result<T> : Result
{
    public T Value { get; }

    protected internal Result(T value, bool isSuccess, Erro? error) : base(isSuccess, error)
    {
        Value = value;
    }

    public static implicit operator Result<T>(Erro error) => Failure<T>(error);
}
