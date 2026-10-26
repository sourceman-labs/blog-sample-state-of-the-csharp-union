using System.Diagnostics.CodeAnalysis;

namespace Domain.After;

public readonly record struct Ok<T>(T Value);
public readonly record struct Err<E>(E Error);

public union Result<T, E>(Ok<T>, Err<E>)
{
    public bool IsOk([MaybeNullWhen(false)] out T value, [MaybeNullWhen(true)] out E error)
    {
        if (Value is Ok<T> ok) { value = ok.Value; error = default; return true; }
        if (Value is Err<E> err) { value = default; error = err.Error; return false; }
        throw new InvalidOperationException("default(Result) holds no value.");
    }
}

public static class Result
{
    public static Ok<T> Ok<T>(T value) => new(value);
    public static Err<E> Err<E>(E error) => new(error);
}
