namespace Domain.After.Composition;

public static class ResultExtensions
{
    extension<T, E>(Result<T, E> result)
    {
        public Result<U, E> Map<U>(Func<T, U> map) =>
            result.IsOk(out var value, out var error) ? Result.Ok(map(value)) : Result.Err(error);

        public Result<U, E> Bind<U>(Func<T, Result<U, E>> bind) =>
            result.IsOk(out var value, out var error) ? bind(value) : Result.Err(error);

        public Result<T, E2> MapErr<E2>(Func<E, E2> map) =>
            result.IsOk(out var value, out var error) ? Result.Ok(value) : Result.Err(map(error));
    }
}
