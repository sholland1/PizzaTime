namespace Hollandsoft.OrderPizza;
public interface ITerminalUI {
    void Print(string message);
    void PrintLine(string message);
    void PrintLine();
    string? ReadLine();
    char? ReadKey();

    string? EditLine(string lineToEdit);

    public string? Prompt(string prompt) {
        Print(prompt);
        var result = ReadLine()?.Trim();
        return string.IsNullOrEmpty(result) ? null : result;
    }

    public char? PromptKey(string prompt) {
        Print(prompt);
        var c = ReadKey();
        PrintLine();
        return c;
    }

    public string? PromptForEdit(string prompt, string lineToEdit) {
        Print(prompt);
        var result = EditLine(lineToEdit);
        return string.IsNullOrEmpty(result) ? null : result;
    }
}

public class RealTerminalUI : ITerminalUI {
    public void Print(string message) => Console.Write(message);
    public void PrintLine(string message) => Console.WriteLine(message);
    public void PrintLine() => Console.WriteLine();

    public char? ReadKey() => Console.ReadKey().KeyChar;
    public string? ReadLine() => Console.ReadLine();

    public string EditLine(string lineToEdit) {
        var offset = Console.CursorLeft;
        Console.Write(lineToEdit);

        List<char> chars = new();
        if (!string.IsNullOrEmpty(lineToEdit)) {
            chars.AddRange(lineToEdit.ToCharArray());
        }

        while (true) {
            var info = Console.ReadKey(true);
            if (info.Key == ConsoleKey.Enter) {
                Console.WriteLine();
                break;
            }

            if (info.Key == ConsoleKey.Backspace && !AtBeginning()) {
                var temp = Console.CursorLeft - 1;
                Console.CursorLeft = offset;
                Console.Write(new string(' ', chars.Count));

                chars.RemoveAt(temp - offset);

                Console.CursorLeft = offset;
                Console.Write(chars.ToArray());
                Console.CursorLeft = temp;
            }
            else if (info.Key == ConsoleKey.Delete && !AtEnd()) {
                var temp = Console.CursorLeft;
                Console.CursorLeft = offset;
                Console.Write(new string(' ', chars.Count));

                chars.RemoveAt(temp - offset);

                Console.CursorLeft = offset;
                Console.Write(chars.ToArray());
                Console.CursorLeft = temp;
            }
            else if (info.Key == ConsoleKey.LeftArrow && !AtBeginning()) {
                Console.CursorLeft -= 1;
            }
            else if (info.Key == ConsoleKey.RightArrow && !AtEnd()) {
                Console.CursorLeft += 1;
            }
            else if (info.Key == ConsoleKey.Home) {
                Console.CursorLeft = offset;
            }
            else if (info.Key == ConsoleKey.End) {
                Console.CursorLeft = chars.Count + offset;
            }
            else if ((info.Modifiers & ConsoleModifiers.Control) != 0) {
                if (info.Key == ConsoleKey.LeftArrow && !AtBeginning()) {
                    //TODO: Move cursor to start of previous word
                }
                else if (info.Key == ConsoleKey.RightArrow && !AtEnd()) {
                    //TODO: Move cursor to start of next word
                }
            }
            else if (!char.IsControl(info.KeyChar)) {
                // else if (char.IsLetterOrDigit(info.KeyChar) || @" !@#$%^&*()_+-=`~[]\{}|;':,./<>?""".Contains(info.KeyChar)) {
                chars.Insert(Console.CursorLeft - offset, info.KeyChar);

                var temp = Console.CursorLeft + 1;
                Console.CursorLeft = offset;
                Console.Write(new string(' ', chars.Count));
                Console.CursorLeft = offset;
                Console.Write(chars.ToArray());
                Console.CursorLeft = temp;
            }
        }

        return new(chars.ToArray());

        bool AtBeginning() => Console.CursorLeft <= offset;
        bool AtEnd() => Console.CursorLeft >= chars.Count + offset;
    }
}
