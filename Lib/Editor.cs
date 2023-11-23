using System.Diagnostics;

namespace Hollandsoft.OrderPizza;

public interface IEditor {
    string? Create();
    string? Edit(string pizzaName, Pizza pizza);
}

public class InstalledProgramEditor : IEditor {
    private readonly string _editor;
    private readonly string _instructions;

    public InstalledProgramEditor(string editor, string instructionsFilename) =>
        (_editor, _instructions) = (editor, File.ReadAllText(instructionsFilename));

    public string? Create() => EditImpl("");

    public string? Edit(string pizzaName, Pizza pizza) => EditImpl($"""

        Editing '{pizzaName}' pizza:
        {pizza.Summarize()}
        ---
        """);

    private string? EditImpl(string prependInstructions) {
        string separator = new('-', 40);
        var filename = GenerateFilename();
        var contents = $"""

            {separator}{prependInstructions}
            {_instructions}
            """;
        File.WriteAllText(filename, contents);
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

    public string? Create() => EditImpl("Describe your new pizza:", Array.Empty<string>());

    public string? Edit(string pizzaName, Pizza pizza) =>
        EditImpl($"Editing pizza:", pizza.Summarize()
            .Split('\n')
            .Append("---")
            .Prepend($"Pizza '{pizzaName}':"));

    private string? EditImpl(string promptMessage, IEnumerable<string> prependInstructions) {
        var editorWidth = Math.Clamp(Console.WindowWidth / 4, promptMessage.Length, 50);

        DrawInstructions(editorWidth, prependInstructions);

        Console.SetCursorPosition(0, 0);

        Console.WriteLine(promptMessage);
        Console.Write("> ");
        var input = Utils.EditLine("", editorWidth);
        Console.Clear();
        return string.IsNullOrWhiteSpace(input) ? null : input;
    }

    private void DrawInstructions(int hPos, IEnumerable<string> prependInstructions) {
        const string divider = " │ ";
        var width = Console.WindowWidth - hPos;
        var lines = prependInstructions
            .Concat(_instructions.Wrap(width - divider.Length))
            .ToList();

        int vPos = 0;
        foreach (var line in lines) {
            Console.SetCursorPosition(hPos, vPos++);
            Console.WriteLine(divider + line);
        }
    }
}
