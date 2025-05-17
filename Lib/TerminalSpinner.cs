using System.Diagnostics;

namespace Hollandsoft.PizzaTime;
public class TerminalSpinner {
    private readonly string _sequence;
    private readonly int _delay;

    public TerminalSpinner(string sequence = "/-\\|", int delay = 50) {
        if (sequence.Length == 0) {
            throw new ArgumentException("Sequence must not be empty", nameof(sequence));
        }
        _sequence = sequence;
        _delay = delay;
    }

    public async Task Show(string message, Action action) =>
        await Show(message, () => { action(); return Task.CompletedTask; });

    public async Task Show(string message, Func<Task> action) =>
        await Show(message, () => { action(); return Task.FromResult(0); });

    public async Task<T> Show<T>(string message, Func<T> action) =>
        await Show(message, () => Task.FromResult(action()));

    public async Task<T> Show<T>(string message, Func<Task<T>> action) {
        var (hPos, vPos) = Console.GetCursorPosition();

        using CancellationTokenSource tokenSource = new();
        var token = tokenSource.Token;
        var t = Task.Run(async () => {
            var result = await action();
            tokenSource.Cancel();
            return result;
        });

        Console.SetCursorPosition(hPos + 2, vPos);
        Console.Write(message);

        Console.CursorVisible = false;
        foreach (var c in _sequence.Cycle()) {
            Console.SetCursorPosition(hPos, vPos);
            Console.Write(c);

            if (token.IsCancellationRequested) {
                Console.SetCursorPosition(hPos, vPos);
                Console.CursorVisible = true;
                return await t;
            }

            await Task.Delay(_delay, CancellationToken.None);
        }
        throw new UnreachableException();
    }
}
