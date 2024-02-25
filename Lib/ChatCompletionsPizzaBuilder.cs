using System.Globalization;
using System.Text.Json;
using OpenAI.Interfaces;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

namespace Hollandsoft.PizzaTime;

using static AIPizzaResultHelpers;

public class ChatCompletionsPizzaBuilder : IAIPizzaBuilder {
    private readonly IChatCompletionService _service;
    private readonly List<ChatMessage> _systemMessages;
    private readonly ISerializer _serializer;
    private readonly FileSystem _fileSystem;

    public ChatCompletionsPizzaBuilder(IOpenAIService service, ISerializer serializer, AIPizzaBuilderConfig config, FileSystem fileSystem) {
        _service = service.ChatCompletion;
        _serializer = serializer;
        _fileSystem = fileSystem;

        var systemMessage = string.Format(CultureInfo.InvariantCulture,
            config.SystemMessage,
            string.Join(' ', ToppingTypeHelpers.AllToppings),
            string.Join(' ', SauceTypeHelpers.AllSauces),
            SizeHelpers.AllowedCrustsAIPrompt);

        var fewShot = _serializer.Deserialize<List<PromptPair>>(config.FewShotText)!;

        _systemMessages = [
            ChatMessage.FromSystem(systemMessage),
            .. fewShot.SelectMany(pp => new[] {
                ChatMessage.FromSystem(pp.User, "example_user"),
                ChatMessage.FromSystem(_serializer.Serialize(pp.Assistant?.Validate()), "example_assistant")
            }),
        ];
    }

    private sealed class PromptPair {
        public string User { get; init; } = "";
        public UnvalidatedPizza? Assistant { get; init; }
    }

    public async Task<AIPizzaResult> CreatePizza(string userCreateMessage) => await EditPizza(null, userCreateMessage);

    public async Task<AIPizzaResult> EditPizza(Pizza? pizza, string userEditMessage) {
        var completionResult = await _service.CreateCompletion(new() {
            Messages = [
                .. _systemMessages,
                ChatMessage.FromAssistant(_serializer.Serialize(pizza)),
                ChatMessage.FromUser(userEditMessage),
            ],
            Model = Models.Gpt_3_5_Turbo,
            MaxTokens = 300,
            Temperature = 0
        });

        if (!completionResult.Successful) {
            return Failure(completionResult.Error?.Message!);
        }
        var result = completionResult.Choices.FirstOrDefault()?.Message?.Content?.Trim();
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
