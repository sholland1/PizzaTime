using System.Text.Json;
using OpenAI.Interfaces;

namespace Hollandsoft.PizzaTime;

using static AIPizzaResultHelpers;

public interface IAIPizzaBuilder {
    Task<AIPizzaResult> CreatePizza(string userCreateMessage);
    Task<AIPizzaResult> EditPizza(Pizza? pizza, string userEditMessage);
}

public class AIPizzaBuilderConfig {
    public string SystemMessageFile { get; init; } = "";
    public string FewShotFile { get; init; } = "";
}

public class CompletionsPizzaBuilder : IAIPizzaBuilder {
    private readonly ICompletionService _service;
    private readonly string _promptPreamble;
    private readonly ISerializer _serializer;
    private readonly FileSystem _fileSystem;

    public CompletionsPizzaBuilder(IOpenAIService service, ISerializer serializer, AIPizzaBuilderConfig config, FileSystem fileSystem) {
        _service = service.Completions;
        _serializer = serializer;
        _fileSystem = fileSystem;

        var systemMessage = _fileSystem.ReadAllText(config.SystemMessageFile);
        var fewShot = _fileSystem.ReadAllText(config.FewShotFile);
        _promptPreamble = string.Format(
            systemMessage,
            string.Join(' ', ToppingTypeHelpers.AllToppings),
            string.Join(' ', SauceTypeHelpers.AllSauces),
            SizeHelpers.AllowedCrustsAIPrompt)
            + "\n+++\n" + fewShot;
    }

    public async Task<AIPizzaResult> CreatePizza(string userCreateMessage) => await EditPizza(null, userCreateMessage);

    public async Task<AIPizzaResult> EditPizza(Pizza? pizza, string userEditMessage) {
        var serialized = _serializer.Serialize(pizza);
        var prompt = _promptPreamble + $"\n\nCurrent: {serialized}\nInput: {userEditMessage}\nOutput: ";

        var completionResult = await _service.CreateCompletion(new() {
            Prompt = prompt,
            Model = "gpt-3.5-turbo-instruct",
            MaxTokens = 300,
            Temperature = 0
        });

        if (!completionResult.Successful) {
            return Failure(completionResult.Error?.Message!);
        }
        var result = completionResult.Choices.FirstOrDefault()?.Text?.Trim();
        if (result is null) return Failure("No result from OpenAI");
        _fileSystem.WriteAllText("AIPizzaDebug.json", result);
        try {
            var deserialized = _serializer.Deserialize<UnvalidatedPizza>(result);
            if (deserialized is null) return Failure("Failed to deserialize pizza");
            var parseResult = deserialized.Parse();
            return parseResult.Match(
                errors => Failure(errors.Select(x => x.ErrorMessage).ToList()),
                Success);
        }
        catch (JsonException ex) {
            return Failure(ex.Message);
        }
    }
}
