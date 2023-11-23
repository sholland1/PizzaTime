namespace Hollandsoft.OrderPizza;
public interface ITerminalUI {
    void Print(string message);
    void PrintLine(string message);
    void PrintLine();
    string? ReadLine();
    char? ReadKey();

    string? EditLine(string lineToEdit);

    void Clear();
    void SetCursorPosition(int left, int top);

    public string? Prompt(string prompt) => PromptForEdit(prompt, "");

    public char? PromptKey(string prompt) {
        Print(prompt);
        var c = ReadKey();
        PrintLine();
        return c;
    }

    public string? PromptForEdit(string prompt, string lineToEdit) {
        Print(prompt);
        var result = EditLine(lineToEdit)?.Trim();
        return string.IsNullOrEmpty(result) ? null : result;
    }
}

public class RealTerminalUI : ITerminalUI {
    public void Print(string message) => Console.Write(message);
    public void PrintLine(string message) => Console.WriteLine(message);
    public void PrintLine() => Console.WriteLine();

    public char? ReadKey() => Console.ReadKey().KeyChar;
    public string? ReadLine() => EditLine("");

    public string EditLine(string lineToEdit) => Utils.EditLine(lineToEdit);

    public void Clear() => Console.Clear();

    public void SetCursorPosition(int left, int top) => Console.SetCursorPosition(left, top);
}
