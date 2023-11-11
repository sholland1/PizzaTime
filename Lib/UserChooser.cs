using System.Net;

namespace Hollandsoft.OrderPizza;

public interface IUserChooser {
    string? GetUserChoice(string prompt, IEnumerable<string> choices, string? itemType = null);
    List<string> GetUserChoices(string prompt, IEnumerable<string> choices, string? itemType = null);
}

public class FzfChooser : IUserChooser {
    private readonly HttpOptions _httpOptions;
    public FzfChooser(HttpOptions httpOptions) => _httpOptions = httpOptions;

    public string? GetUserChoice(string prompt, IEnumerable<string> choices, string? itemType = null) =>
        choices.ChooseWithFzf(new Fzf.FzfOptions {
            Prompt = prompt,
            Preview = GetPreviewCommand(itemType)
        });

    public List<string> GetUserChoices(string prompt, IEnumerable<string> choices, string? itemType = null) =>
        choices.ChooseWithFzf(new Fzf.FzfMultiOptions {
            Prompt = prompt,
            Preview = GetPreviewCommand(itemType)
        });

    private string? GetPreviewCommand(string? itemType) => itemType is null ? null
        : $"echo -n '{itemType}:{{}}' | nc {_httpOptions.IPAddress} {_httpOptions.Port}";
}

public record HttpOptions(IPAddress IPAddress, int Port);
