using System.Diagnostics;

namespace Hollandsoft.PizzaTime;

public interface IEditor {
    string? Create();
    string? Edit(string pizzaName, Pizza pizza);
}

public class InstalledProgramEditor(string _editor, FileSystem _fileSystem, string _instructionsFilename) : IEditor {
    private readonly string _instructions = string.Format(
        _fileSystem.ReadAllText(_instructionsFilename),
        SizeHelpers.AllowedCrustsUserInstructions,
        string.Join(", ", CutHelpers.AllCuts),
        string.Join(", ", BakeHelpers.AllBakes),
        string.Join(", ", SauceTypeHelpers.AllSauces),
        string.Join('\n', ToppingTypeHelpers.AllToppings));

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
        _fileSystem.WriteAllText(filename, contents);
        Process.Start(_editor, filename).WaitForExit();
        var lines = _fileSystem.ReadLines(filename)
            .TakeWhile(s => s != separator)
            .ToArray();
        _fileSystem.Delete(filename);

        return lines is [""] ? null
            : string.Join(Environment.NewLine, lines);
    }

    private static string GenerateFilename() => "PIZZA_EDITMSG";
}

public class FallbackEditor(FileSystem _fileSystem, string _instructionsFilename) : IEditor {
    private readonly string[] _instructions = string.Format(
        _fileSystem.ReadAllText(_instructionsFilename),
        SizeHelpers.AllowedCrustsUserInstructions,
        string.Join(", ", CutHelpers.AllCuts),
        string.Join(", ", BakeHelpers.AllBakes),
        string.Join(", ", SauceTypeHelpers.AllSauces),
        string.Join('\n', ToppingTypeHelpers.AllToppings)).Split('\n');

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

    private void DrawInstructions(int hPos, IEnumerable<string> prependInstructions) =>
        Utils.WriteInfoPanel(hPos, prependInstructions.Concat(_instructions));
}
