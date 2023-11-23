using System.Diagnostics;

namespace Hollandsoft.OrderPizza;

public interface IEditor {
    string? Edit();
}

public class InstalledProgramEditor : IEditor {
    private readonly string _editor;
    private readonly string _instructions;

    public InstalledProgramEditor(string editor, string instructionsFilename) =>
        (_editor, _instructions) = (editor, File.ReadAllText(instructionsFilename));

    public string? Edit() {
        string separator = new('-', 40);
        var filename = GenerateFilename();

        File.WriteAllText(filename, $"\n{separator}\n\n{_instructions}");
        Process.Start(_editor, filename).WaitForExit();
        var lines = File.ReadAllLines(filename)
            .TakeWhile(s => s != separator)
            .ToArray();
        File.Delete(filename);

        return lines is [""] ? null
            : string.Join(Environment.NewLine, lines);
    }

    //TODO: Use something like .pizza/COMMIT_EDITMSG
    private static string GenerateFilename() => Path.GetTempFileName();
}

public class FallbackEditor : IEditor {
    private readonly string[] _instructions;

    public FallbackEditor(string instructionsFilename) =>
        _instructions = File.ReadAllLines(instructionsFilename);

    public string? Edit() {
        var editorWidth = 50;

        var hPos = editorWidth;
        var width = Console.WindowWidth - hPos;
        var lines = _instructions.Wrap(width - 3).ToList();

        int vPos = 0;
        foreach (var line in lines) {
            Console.SetCursorPosition(hPos, vPos++);
            Console.WriteLine(" | " + line);
        }

        Console.SetCursorPosition(0, 0);

        Console.WriteLine("Describe your new pizza:");
        Console.Write("> ");
        var input = Utils.EditLine("", editorWidth);
        Console.Clear();
        return string.IsNullOrWhiteSpace(input) ? null : input;
    }
}
