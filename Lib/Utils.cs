using System.Diagnostics;
using FluentValidation.Results;

static class Utils {
    public static bool In<T>(this T source, params T[] items) => items.Contains(source);

    public static bool Between<T>(this T source, T a, T b) where T : IComparable<T> =>
        source.CompareTo(a) >= 0 && source.CompareTo(b) <= 0;

    public static bool ContainsDuplicates<T>(this IEnumerable<T> source) {
        HashSet<T> knownKeys = new();
        return source.Any(k => !knownKeys.Add(k));
    }

    public static bool MatchesMMyy(string s) =>
        s.Length == 5
        && int.TryParse(s[..2], out var month)
        && s[2] == '/'
        && int.TryParse(s[3..], out var _year)
        && month.Between(1, 12);
}

public abstract record Validation<T> {
    public sealed record Failure(List<ValidationFailure> Value) : Validation<T>;
    public sealed record Success(T Value) : Validation<T>;

    public R Match<R>(Func<T, R> success, Func<List<ValidationFailure>, R> failure) => this switch {
        Success s => success(s.Value),
        Failure f => failure(f.Value),
        _ => throw new UnreachableException($"Invalid Result! {this}")
    };

    public void Match(Action<T> success, Action<List<ValidationFailure>> failure) =>
        Match(
            x => { success(x); return 0; },
            x => { failure(x); return 1; });
}

public abstract record Result<TFailure, TSuccess> {
    public sealed record Failure(TFailure Value) : Result<TFailure, TSuccess>;
    public sealed record Success(TSuccess Value) : Result<TFailure, TSuccess>;

    public T Match<T>(Func<TSuccess, T> success, Func<TFailure, T> failure) => this switch {
        Success s => success(s.Value),
        Failure f => failure(f.Value),
        _ => throw new UnreachableException($"Invalid Result! {this}")
    };

    public void Match(Action<TSuccess> success, Action<TFailure> failure) =>
        Match(
            x => { success(x); return 0; },
            x => { failure(x); return 1; });
}

public static class ResultHelpers {
    public static Result<E, U> SelectMany<E, T, U>(this Result<E, T> source, Func<T, Result<E, U>> selector) =>
        source.Match(selector, f => new Result<E, U>.Failure(f));

    public static Result<E, U> SelectMany<E, C, T, U>(this Result<E, T> source, Func<T, Result<E, C>> selector,  Func<T, C, U> projector) => 
        source.Match(
            s => selector(s).Match<Result<E, U>>(
                ss => new Result<E, U>.Success(projector(s, ss)),
                ff => new Result<E, U>.Failure(ff)),
            f => new Result<E, U>.Failure(f));

    public static Result<E, U> Select<E, T, U>(this Result<E, T> source, Func<T, U> selector) =>
        source.Match<Result<E, U>>(
            s => new Result<E, U>.Success(selector(s)),
            f => new Result<E, U>.Failure(f));

    public static void MapFailure<E, T>(this Result<E, T> source, Action<E> selector) =>
        source.Match(s => { }, selector);

    public static Result<F, T> MapFailure<E, F, T>(this Result<E, T> source, Func<E, F> selector) =>
        source.Match<Result<F, T>>(
            s => new Result<F, T>.Success(s),
            f => new Result<F, T>.Failure(selector(f)));
}