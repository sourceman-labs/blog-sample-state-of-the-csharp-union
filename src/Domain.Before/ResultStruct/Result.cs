namespace Domain.Before.ResultStruct;

public readonly struct Result<T, E>
{
    private readonly T? _value;
    private readonly E? _error;

    private Result(T value) => (_value, IsOk) = (value, true);
    private Result(E error) => _error = error;

    public bool IsOk { get; }

    public static Result<T, E> Ok(T value) => new(value);
    public static Result<T, E> Err(E error) => new(error);

    public static implicit operator Result<T, E>(T value) => new(value);
    public static implicit operator Result<T, E>(E error) => new(error);

    public T Value => IsOk ? _value! : throw new InvalidOperationException("Result is Err.");
    public E Error => !IsOk ? _error! : throw new InvalidOperationException("Result is Ok.");

    public TOut Match<TOut>(Func<T, TOut> ok, Func<E, TOut> err) => IsOk ? ok(_value!) : err(_error!);
}
