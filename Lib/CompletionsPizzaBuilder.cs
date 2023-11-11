using System.Text.Json;
using OpenAI.Interfaces;

namespace Hollandsoft.OrderPizza;

using static AIPizzaResultHelpers;

public interface IAIPizzaBuilder {
    Task<AIPizzaResult> CreatePizza(string input);
    Task<AIPizzaResult> EditPizza(Pizza? pizza, string input);
}

public class AIPizzaBuilderConfig {
    public string SystemMessageFile { get; set; } = "";
    public string FewShotFile { get; set; } = "";
}

public class CompletionsPizzaBuilder : IAIPizzaBuilder {
    private readonly ICompletionService _service;
    private readonly string _promptPreamble;

    public CompletionsPizzaBuilder(IOpenAIService service, AIPizzaBuilderConfig config) {
        _service = service.Completions;

        var systemMessage = File.ReadAllText(config.SystemMessageFile);
        var fewShot = File.ReadAllText(config.FewShotFile);
        _promptPreamble = string.Format(
            systemMessage,
            ToppingTypeHelpers.AllToppingsString,
            SauceTypeHelpers.AllSaucesString,
            SizeHelpers.AllowedCrustsString)
            + "\n+++\n" + fewShot;
    }

    public async Task<AIPizzaResult> CreatePizza(string userCreateMessage) => await EditPizza(null, userCreateMessage);

    public async Task<AIPizzaResult> EditPizza(Pizza? pizza, string userEditMessage) {
        var serialized = JsonSerializer.Serialize(pizza, PizzaSerializer.Options);
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
        File.WriteAllText("AIPizza.json", result);
        try {
            var deserialized = JsonSerializer.Deserialize<UnvalidatedPizza>(result, PizzaSerializer.Options);
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
