using System.Diagnostics;
using FluentValidation.Results;

namespace Hollandsoft.OrderPizza;
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
        && int.TryParse(s[3..], out _)
        && month.Between(1, 12);
}

public abstract record Validation<T> {
    public sealed record Failure(List<ValidationFailure> Value) : Validation<T>;
    public sealed record Success(T Value) : Validation<T>;

    public T? ToNullable() => this switch {
        Success s => s.Value,
        _ => default
    };

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
