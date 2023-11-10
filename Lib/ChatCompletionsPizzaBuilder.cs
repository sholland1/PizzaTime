using System.Text.Json;
using OpenAI.Interfaces;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

namespace Hollandsoft.OrderPizza;

using static AIPizzaResultHelpers;

public class ChatCompletionsPizzaBuilder : IAIPizzaBuilder {
    private readonly IChatCompletionService _service;
    private readonly List<ChatMessage> _systemMessages;

    public ChatCompletionsPizzaBuilder(IOpenAIService service, AIPizzaBuilderConfig config) {
        _service = service.ChatCompletion;

        var systemMessage = string.Format(
            File.ReadAllText(config.SystemMessageFile),
            ToppingTypeHelpers.AllToppingsString,
            SauceTypeHelpers.AllSaucesString,
            SizeHelpers.AllowedCrustsString);

        var fewShot = JsonSerializer.Deserialize<List<PromptPair>>(
            File.ReadAllText(config.FewShotFile), PizzaSerializer.Options)!;

        _systemMessages = fewShot
            .SelectMany(pp => pp.Messages)
            .Prepend(System(systemMessage))
            .ToList();
    }

    private class PromptPair {
        public string User { get; set; } = "";
        public UnvalidatedPizza? Assistant { get; set; }
        public IEnumerable<ChatMessage> Messages => new[] {
            SystemUser(User),
            SystemAssistant(Assistant?.Validate())
        };
    }

    private static ChatMessage System(string message) => ChatMessage.FromSystem(message);
    private static ChatMessage SystemUser(string message) => ChatMessage.FromSystem(message, "example_user");
    private static ChatMessage SystemAssistant(Pizza? pizza) =>
        ChatMessage.FromSystem(
            JsonSerializer.Serialize(pizza, PizzaSerializer.Options),
            "example_assistant");
    private static ChatMessage User(string message) => ChatMessage.FromUser(message);
    private static ChatMessage Assistant(Pizza? pizza) =>
        ChatMessage.FromAssistant(
            JsonSerializer.Serialize(pizza, PizzaSerializer.Options));

    public async Task<AIPizzaResult> NewPizza(string input) => await EditPizza(null, input);

    public async Task<AIPizzaResult> EditPizza(Pizza? pizza, string input) {
        var completionResult = await _service.CreateCompletion(new() {
            Messages = _systemMessages
                .Append(Assistant(pizza))
                .Append(User(input))
                .ToList(),
            Model = Models.Gpt_3_5_Turbo,
            MaxTokens = 300,
            Temperature = 0
        });

        if (!completionResult.Successful) {
            return Failure(completionResult.Error?.Message!);
        }
        var result = completionResult.Choices.FirstOrDefault()?.Message?.Content?.Trim();
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
