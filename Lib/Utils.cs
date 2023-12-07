using System.Diagnostics;
using FluentValidation.Results;

namespace Hollandsoft.OrderPizza;
public static partial class Utils {
    public static bool In<T>(this T source, params T[] items) => items.Contains(source);

    public static bool Between<T>(this T source, T a, T b) where T : IComparable<T> =>
        source.CompareTo(a) >= 0 && source.CompareTo(b) <= 0;

    public static bool ContainsDuplicates<T>(this IEnumerable<T> source) {
        HashSet<T> knownKeys = [];
        return source.Any(k => !knownKeys.Add(k));
    }

    public static bool MatchesMMyy(string s) =>
        s.Length == 5
        && int.TryParse(s[..2], out var month)
        && s[2] == '/'
        && int.TryParse(s[3..], out _)
        && month.Between(1, 12);

    public static IEnumerable<string> Wrap(this IEnumerable<string> source, int maxLength, int firstLineOffset = 0) {
        return source.SelectMany(s => WrapLine(s, firstLineOffset));

        // IEnumerable<string> WrapLine(string line) =>
        //     line.Length <= maxLength
        //         ? [line]
        //         : [line[..maxLength], ...WrapLine(line[(maxLength+1)..])];

        IEnumerable<string> WrapLine(string line, int offset = 0) {
            if (offset + line.Length <= maxLength) {
                yield return line;
                yield break;
            }
            var (first, rest) = (line[..(maxLength-offset)], line[(maxLength-offset)..]);

            yield return first;
            foreach (var l in WrapLine(rest)) {
                yield return l;
            }
        }
    }

    public static IEnumerable<T> Cycle<T>(this IEnumerable<T> source) {
        while (true) {
            foreach (var item in source) {
                yield return item;
            }
        }
    }

    private static readonly char[] _invalidChars = Path
        .GetInvalidFileNameChars()
        .Concat("`$&*()[]{}\\|:;\"'<>?/") //zsh/netcat don't like these
        .Distinct()
        .ToArray();
    public static bool IsValidName(this string filename) => !filename.Any(_invalidChars.Contains);

    public static (string, string) SplitAtFirst(string s, char c) {
        var index = s.IndexOf(c);
        return index == -1 ? (s, "") : (s[..index], s[(index + 1)..]);
    }

    public static DateTime TruncateToSeconds(this DateTime d) =>
        new(d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second);
}

public abstract record Validation<T> {
    public sealed record Failure(List<ValidationFailure> Value) : Validation<T>;
    public sealed record Success(T Value) : Validation<T>;

    public T? ToNullable() => this switch {
        Success s => s.Value,
        _ => default
    };

    public TResult Match<TResult>(Func<List<ValidationFailure>, TResult> failure, Func<T, TResult> success) => this switch {
        Failure f => failure(f.Value),
        Success s => success(s.Value),
        _ => throw new UnreachableException($"Invalid Result! {this}")
    };

    public void Match(Action<T> success, Action<List<ValidationFailure>> failure) =>
        Match(
            x => { failure(x); return 1; },
            x => { success(x); return 0; });
}
