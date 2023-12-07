using System.ComponentModel;
using System.Net;
using Controllers;
using Hollandsoft.OrderPizza;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using OpenAI.Extensions;
using Server;
using Spectre.Console;
using Spectre.Console.Cli;

var services = BuildServiceProvider();
App.TypeRegistrar registrar = new(services);
CommandApp<DefaultCommand> app = new(registrar);

app.Configure(config =>
    config.SetExceptionHandler(ex =>
        Console.WriteLine($"Unhandled exception: {ex}")));

await app.RunAsync(args);

static ServiceCollection BuildServiceProvider() {
    ConfigurationBuilder builder = new();
    var configuration = builder
        .AddJsonFile("appsettings.json")
        .AddUserSecrets<Program>()
        .AddEnvironmentVariables(static _ => Environment.SetEnvironmentVariable(
            "OpenAIServiceOptions:ApiKey", Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
        .Build();

    var dotnetEnv = configuration.GetValue<string>("DOTNET_ENVIRONMENT");

    var openAiApiKey = configuration.GetValue<string>("OpenAIServiceOptions:ApiKey");
    if (openAiApiKey is null){
        Console.WriteLine(GetMissingApiKeyMessage(dotnetEnv));
        Environment.Exit(1);
    }

    ServiceCollection services = new();
    services.AddSingleton<IConfiguration>(configuration);
    services.AddSingleton(new HttpOptions(
        IPAddress.Parse(configuration.GetValue<string>("PizzaQueryServer:Host")!),
        configuration.GetValue<int>("PizzaQueryServer:Port")));

    services.AddOpenAIService();

    var dataRootDir = GetDataRootDir(dotnetEnv, "OrderPizza");
    services.AddSingleton(new FileSystem(dataRootDir));

    services.AddLogging(loggingBuilder =>
        loggingBuilder
            .ClearProviders()
            .AddNLog(configuration))
        .AddSingleton<ISerializer>(MyJsonSerializer.Instance)
        .AddSingleton<IOrderApi, DominosOrderApi>()
        .AddSingleton<IStoreApi, DominosStoreApi>()
        .AddSingleton<IPizzaRepo, JsonFilePizzaRepository>()
        .AddSingleton<Func<OrderInfo, ICart>>(services =>
        o => new DominosCart(
            services.GetRequiredService<IOrderApi>()!, o))

        .AddSingleton(new AIPizzaBuilderConfig {
            SystemMessageFile = "AIPizzaPromptSystemMessage.txt",
            FewShotFile = "FewShotPrompt.json"
        })
        .AddSingleton<IAIPizzaBuilder, ChatCompletionsPizzaBuilder>()

        .AddSingleton<ITerminalUI, RealTerminalUI>()
        .AddSingleton<IUserChooser, FzfChooser>()
        .AddSingleton<TerminalSpinner>()
        .AddSingleton<IDateGetter, CurrentDateGetter>();

    var editor = configuration.GetValue<string>("EDITOR");
    services.AddSingleton<IEditor>(editor is not null
        ? new InstalledProgramEditor(editor, "AIPizzaPromptSystemMessage.txt")
        : new FallbackEditor("AIPizzaPromptSystemMessage.txt"));

    services
        .AddSingleton<PizzaController>()
        .AddSingleton<PizzaQueryServer>();

    return services;
}

static string GetMissingApiKeyMessage(string? dotnetEnv) => dotnetEnv == "Development"
    ? """
    Please set the OpenAI API Key in user secrets:

    $ dotnet user-secrets set "OpenAIServiceOptions:ApiKey" "<your-key>"
    """
    : Environment.OSVersion.Platform switch {
        //Windows
        PlatformID.Win32NT => """
            Please set the OPENAI_API_KEY environment variable:

            > set OPENAI_API_KEY=<your-key>
            """,

        //Unix
        PlatformID.Unix => """
            Please set the OPENAI_API_KEY environment variable:

            $ export OPENAI_API_KEY='<your-key>'

            Place this command in your shell's startup file to make it permanent.
            """,

        //Fallback
        _ => "Please set the OPENAI_API_KEY environment variable.",
    };

static string GetDataRootDir(string? dotnetEnv, string programName) {
    if (dotnetEnv == "Development") return ".";

    var dataDirectory =
        Environment.OSVersion.Platform switch {
            //Windows
            PlatformID.Win32NT =>
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), programName),

            //Unix
            PlatformID.Unix => Path.Combine(
                Environment.GetEnvironmentVariable("XDG_DATA_HOME")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".local", "share"),
                programName),

            //Fallback
            _ => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), programName),
        };

    Directory.CreateDirectory(dataDirectory);

    return dataDirectory;
}

internal sealed class DefaultCommand(PizzaQueryServer _server, PizzaController _controller) : AsyncCommand<DefaultCommand.Settings> {
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings) =>
        await PizzaMain(settings.DefaultOrder, settings.OrderName, settings.Track);

    public sealed class Settings : CommandSettings {
        [CommandOption("--default-order")]
        [Description("Place the default order with user confirmation only")]
        public bool DefaultOrder { get; set; }

        [CommandOption("---order <NAME>")]
        [Description("Place the order with the specified name with user confirmation only")]
        public string? OrderName { get; set; }

        [CommandOption("--track")]
        [Description("Track your recent order")]
        public bool Track { get; set; }

        public override ValidationResult Validate() =>
            DefaultOrder && OrderName is not null
            || DefaultOrder && Track
            || OrderName is not null && Track
                ? ValidationResult.Error("All arguments are mutually exclusive.")
                : base.Validate();
    }

    private async Task<int> PizzaMain(bool defaultOrder, string? orderName, bool track) {
        if (defaultOrder) {
            await _controller.PlaceDefaultOrder();
            return 0;
        }

        if (orderName is not null) {
            await _controller.PlaceOrder(orderName);
            return 0;
        }

        if (track) {
            await _controller.TrackOrder();
            return 0;
        }

        _ = _server.StartServer();
        await _controller.OpenProgram();
        return 0;
    }
}
