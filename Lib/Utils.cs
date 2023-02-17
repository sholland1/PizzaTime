using System.Diagnostics;

static class Utils {
    public static bool In<T>(this T source, params T[] items) => items.Contains(source);

    public static bool Between<T>(this T source, T a, T b) where T : IComparable<T> =>
        source.CompareTo(a) >= 0 && source.CompareTo(b) <= 0;

    public static bool ContainsDuplicates<T>(this IEnumerable<T> source) {
        HashSet<T> knownKeys = new();
        return source.Any(k => !knownKeys.Add(k));
    }
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
        Match<int>(
            x => { success(x); return 0; },
            x => { failure(x); return 1; });
}