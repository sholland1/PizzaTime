using System.Globalization;
using System.Text.Json;
using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

namespace Hollandsoft.PizzaTime;

using static AIPizzaResultHelpers;

public class AnthropicPizzaBuilder : IAIPizzaBuilder {
    private readonly MessagesEndpoint _client;
    private readonly string _systemMessage;
    private readonly List<Message> _otherMessages;
    private readonly ISerializer _serializer;
    private readonly FileSystem _fileSystem;

    public AnthropicPizzaBuilder(AnthropicClient client, ISerializer serializer, AIPizzaBuilderConfig config, FileSystem fileSystem) {
        _client = client.Messages;
        _serializer = serializer;
        _fileSystem = fileSystem;

        _systemMessage = string.Format(CultureInfo.InvariantCulture,
            config.SystemMessage,
            string.Join(' ', ToppingTypeHelpers.AllToppings),
            string.Join(' ', SauceTypeHelpers.AllSauces),
            SizeHelpers.AllowedCrustsAIPrompt);

        var fewShot = _serializer.Deserialize<List<PromptPair>>(config.FewShotText)!;

        _otherMessages = fewShot.SelectMany(pp => new[] {
            new Message { Role = RoleType.User, Content = pp.User },
            new() { Role = RoleType.Assistant, Content = _serializer.Serialize(pp.Assistant?.Validate()) }
        }).ToList();
    }

    private sealed class PromptPair {
        public string User { get; init; } = "";
        public UnvalidatedPizza? Assistant { get; init; }
    }

    public async Task<AIPizzaResult> CreatePizza(string userCreateMessage) => await EditPizza(null, userCreateMessage);

    public async Task<AIPizzaResult> EditPizza(Pizza? pizza, string userEditMessage) {
        List<Message> messages = new(_otherMessages);
        if (pizza is not null) {
            messages.Add(new() { Role = RoleType.User, Content = "Pizza from file" });
            messages.Add(new() { Role = RoleType.Assistant, Content = _serializer.Serialize(pizza) });
        };
        messages.Add(new() { Role = RoleType.User, Content = userEditMessage });

        MessageParameters parameters = new() {
            SystemMessage = _systemMessage,
            Messages = messages,
            MaxTokens = 300,
            Model = AnthropicModels.Claude3Sonnet,
            Stream = false,
            Temperature = 0
        };

        var res = await _client.GetClaudeMessageAsync(parameters);

        var result = res.Content.FirstOrDefault()?.Text;
        if (result is null) return Failure("No result from Anthropic");
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
