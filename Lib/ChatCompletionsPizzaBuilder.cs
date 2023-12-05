using System.Text.Json;
using OpenAI.Interfaces;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

namespace Hollandsoft.OrderPizza;

using static AIPizzaResultHelpers;

public class ChatCompletionsPizzaBuilder : IAIPizzaBuilder {
    private readonly IChatCompletionService _service;
    private readonly List<ChatMessage> _systemMessages;
    private readonly ISerializer _serializer;

    public ChatCompletionsPizzaBuilder(IOpenAIService service, ISerializer serializer, AIPizzaBuilderConfig config) {
        _service = service.ChatCompletion;
        _serializer = serializer;

        var systemMessage = string.Format(
            File.ReadAllText(config.SystemMessageFile),
            ToppingTypeHelpers.AllToppingsString,
            SauceTypeHelpers.AllSaucesString,
            SizeHelpers.AllowedCrustsString);

        var fewShot = _serializer.Deserialize<List<PromptPair>>(
            File.ReadAllText(config.FewShotFile))!;

        _systemMessages = fewShot
            .SelectMany(pp => new[] {
                ChatMessage.FromSystem(pp.User, "example_user"),
                ChatMessage.FromSystem(_serializer.Serialize(pp.Assistant?.Validate()), "example_assistant")
            })
            .Prepend(ChatMessage.FromSystem(systemMessage))
            .ToList();
    }

    private sealed class PromptPair {
        public string User { get; set; } = "";
        public UnvalidatedPizza? Assistant { get; set; }
    }

    public async Task<AIPizzaResult> CreatePizza(string userCreateMessage) => await EditPizza(null, userCreateMessage);

    public async Task<AIPizzaResult> EditPizza(Pizza? pizza, string userEditMessage) {
        var completionResult = await _service.CreateCompletion(new() {
            Messages = _systemMessages
                .Append(ChatMessage.FromAssistant(_serializer.Serialize(pizza)))
                .Append(ChatMessage.FromUser(userEditMessage))
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
        File.WriteAllText("AIPizzaDebug.json", result);
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
