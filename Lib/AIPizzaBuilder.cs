using System.Text.Json;
using OpenAI.Interfaces;
using OpenAI.ObjectModels;

namespace Hollandsoft.OrderPizza;

using static AIPizzaResultHelpers;

public class AIPizzaBuilderConfig {
    public string NewPizzaPromptPreambleFile { get; set; } = "";
    public string EditPizzaPromptPreambleFile { get; set; } = "";
}

public class AIPizzaBuilder {
    private readonly ICompletionService _service;
    private readonly string _newPizzaPromptPreamble;
    private readonly string _editPizzaPromptPreamble;

    public AIPizzaBuilder(ICompletionService service, AIPizzaBuilderConfig config) {
        _service = service;

        _newPizzaPromptPreamble = File.ReadAllText(config.NewPizzaPromptPreambleFile);
        _editPizzaPromptPreamble = File.ReadAllText(config.EditPizzaPromptPreambleFile);
    }

    public async Task<AIPizzaResult> NewPizza(string input) {
        var prompt = _newPizzaPromptPreamble + $"\n\nInput: {input}\nOutput: ";
        return await RunPrompt(prompt);
    }

    public async Task<AIPizzaResult> EditPizza(Pizza pizza, string input) {
        var serialized = JsonSerializer.Serialize(pizza, PizzaSerializer.Options);
        var prompt = _editPizzaPromptPreamble + $"\n\nCurrent: {serialized}\nInput: {input}\nOutput: ";

        return await RunPrompt(prompt);
    }

    private async Task<AIPizzaResult> RunPrompt(string prompt) {
        var completionResult = await _service.CreateCompletion(new() {
            Prompt = prompt,
            Model = Models.TextDavinciV3,
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
                Success,
                errors => Failure(errors.Select(x => x.ErrorMessage).ToList()));
        }
        catch (JsonException ex) {
            return Failure(ex.Message);
        }
    }
}

public static class AIPizzaResultHelpers {
    public static AIPizzaResult Success(Pizza value) => new AIPizzaResult.Success(value);
    public static AIPizzaResult Failure(string message) => new AIPizzaResult.Failure(new() { message });
    public static AIPizzaResult Failure(List<string> messages) => new AIPizzaResult.Failure(messages);
}

public abstract record AIPizzaResult {
    internal record Failure(List<string> Messages) : AIPizzaResult;
    internal record Success(Pizza Value) : AIPizzaResult;

    public bool IsSuccess => this is Success;
    public bool IsFailure => this is Failure;

    public List<string>? FailureMessages => (this as Failure)?.Messages;
    public Pizza? SuccessValue => (this as Success)?.Value;

    public T Match<T>(Func<List<string>, T> failure, Func<Pizza, T> success) =>
        this switch {
            Failure f => failure(f.Messages),
            Success s => success(s.Value),
            _ => throw new NotImplementedException()
        };

    public void Match(Action<List<string>> failure, Action<Pizza> success) {
        switch (this) {
            case Failure f:
                failure(f.Messages);
                break;
            case Success s:
                success(s.Value);
                break;
            default:
                throw new NotImplementedException();
        }
    }
}
