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

public class RealConsoleUI : IConsoleUI {
    public void Print(string message) => Console.Write(message);
    public void PrintLine(string message) => Console.WriteLine(message);
    public void PrintLine() => Console.WriteLine();

    public string? ReadLine() => Console.ReadLine();
}