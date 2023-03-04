public interface IConsoleUI {
    void Print(string message);
    void PrintLine(string message);
    void PrintLine();
    string? ReadLine();

    public string? Prompt(string prompt) {
        Print(prompt);
        var result = ReadLine()?.Trim();
        return string.IsNullOrEmpty(result) ? null : result;
    }
}

public class DummyConsoleUI : IConsoleUI {
    public List<string> PrintedMessages = new List<string>();
    private readonly Queue<string> _readLines = new Queue<string>();

    public DummyConsoleUI(params string[] readLines) => Array.ForEach(readLines, _readLines.Enqueue);

    public void Print(string message) => PrintedMessages.Add(message);
    public void PrintLine(string message) => PrintedMessages.Add(message + "\n");
    public void PrintLine() => PrintedMessages.Add("\n");
    public string? ReadLine() => _readLines.Dequeue();

    public override string ToString() => string.Join("", PrintedMessages);
}

public class RealConsoleUI : IConsoleUI {
    public void Print(string message) => Console.Write(message);
    public void PrintLine(string message) => Console.WriteLine(message);
    public void PrintLine() => Console.WriteLine();

    public string? ReadLine() => Console.ReadLine();
}