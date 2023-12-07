namespace Hollandsoft.PizzaTime;

public abstract record AIPizzaResult {
    internal sealed record Failure(List<string> Messages) : AIPizzaResult;
    internal sealed record Success(Pizza Value) : AIPizzaResult;

    public bool IsSuccess => this is Success;
    public bool IsFailure => this is Failure;

    public List<string>? FailureMessages => (this as Failure)?.Messages;
    public Pizza? SuccessValue => (this as Success)?.Value;

    public T Match<T>(Func<List<string>, T> failure, Func<Pizza, T> success) =>
        this switch {
            Failure f => failure(f.Messages),
            Success s => success(s.Value),
            _ => throw new NotImplementedException()
        };

    public async Task<T> Match<T>(Func<List<string>, T> failure, Func<Pizza, Task<T>> success) =>
        this switch {
            Failure f => failure(f.Messages),
            Success s => await success(s.Value),
            _ => throw new NotImplementedException()
        };

    public async Task<T> Match<T>(Func<List<string>, Task<T>> failure, Func<Pizza, T> success) =>
        this switch {
            Failure f => await failure(f.Messages),
            Success s => success(s.Value),
            _ => throw new NotImplementedException()
        };

    public async Task<T> Match<T>(Func<List<string>, Task<T>> failure, Func<Pizza, Task<T>> success) =>
        this switch {
            Failure f => await failure(f.Messages),
            Success s => await success(s.Value),
            _ => throw new NotImplementedException()
        };

    public void Match(Action<List<string>> failure, Action<Pizza> success) {
        switch (this) {
            case Failure f:
                failure(f.Messages);
                break;
            case Success s:
                success(s.Value);
                break;
            default:
                throw new NotImplementedException();
        }
    }
}

public static class AIPizzaResultHelpers {
    public static AIPizzaResult Success(Pizza value) => new AIPizzaResult.Success(value);
    public static AIPizzaResult Failure(string message) => new AIPizzaResult.Failure([message]);
    public static AIPizzaResult Failure(List<string> messages) => new AIPizzaResult.Failure(messages);
}
