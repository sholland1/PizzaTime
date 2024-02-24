using System.ComponentModel;
using System.Net;
using Controllers;
using Hollandsoft.PizzaTime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using OpenAI.Extensions;
using Server;
using Spectre.Console;
using Spectre.Console.Cli;

var services = BuildServiceCollection();
App.TypeRegistrar registrar = new(services);
CommandApp<DefaultCommand> app = new(registrar);

app.Configure(config =>
    config.SetExceptionHandler(ex =>
        Console.WriteLine($"Unhandled exception: {ex}")));

return await app.RunAsync(args);

static ServiceCollection BuildServiceCollection() {
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

    HttpOptions httpOptions = new(
        IPAddress.Parse(configuration.GetValue<string>("PizzaQueryServer:Host")!),
        configuration.GetValue<int>("PizzaQueryServer:Port"));
    services.AddSingleton(httpOptions);

    services.AddOpenAIService();

    var dataRootDir = GetDataRootDir(dotnetEnv, "PizzaTime");
    services.AddSingleton(new FileSystem(dataRootDir));

    var configRootDir = AppDomain.CurrentDomain.BaseDirectory;
    FileSystem configFs = new(configRootDir);

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
            SystemMessage = configFs.ReadAllText("AIPizzaPromptSystemMessage.txt"),
            FewShotText = configFs.ReadAllText("FewShotPrompt.json")
        })
        .AddSingleton<IAIPizzaBuilder, ChatCompletionsPizzaBuilder>()

        .AddSingleton<ITerminalUI, RealTerminalUI>()
        .AddSingleton<IUserChooser, FzfChooser>()
        .AddSingleton<TerminalSpinner>()
        .AddSingleton<IDateGetter, CurrentDateGetter>();

    var editor = configuration.GetValue<string>("EDITOR");
    services.AddSingleton<IEditor>(services => {
        var fs = services.GetRequiredService<FileSystem>();
        var instructions = configFs.ReadAllText("InstructionsToDescribePizza.txt");
        return editor is not null
            ? new InstalledProgramEditor(editor, fs, instructions)
            : new FallbackEditor(instructions);
    });

    services
        .AddSingleton(new DebugInfo {
            OpenAiApiKey = openAiApiKey,
            DataRootDir = dataRootDir,
            ConfigRootDir = configRootDir,
            DotnetEnv = dotnetEnv,
            Editor = editor,
            IPAddress = httpOptions.IPAddress,
            Port = httpOptions.Port
        })
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

static string GetDataRootDir(string? dotnetEnv, string programName) =>
    dotnetEnv == "Development"
        ? "."
        : Environment.OSVersion.Platform switch {
            //Unix
            PlatformID.Unix => Path.Combine(
                Environment.GetEnvironmentVariable("XDG_DATA_HOME")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share"),
                programName),

            //Windows or Fallback
            PlatformID.Win32NT or _ =>
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), programName),
        };

internal sealed class DefaultCommand(PizzaQueryServer _server, PizzaController _controller, DebugInfo _debugInfo) : AsyncCommand<DefaultCommand.Settings> {
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings) =>
        await PizzaMain(settings.DefaultOrder, settings.OrderName, settings.Track, settings.Debug);

    public sealed class Settings : CommandSettings {
        [CommandOption("--default-order")]
        [Description("Place the default order with user confirmation only")]
        public bool DefaultOrder { get; init; }

        [CommandOption("--order <NAME>")]
        [Description("Place the order with the specified name with user confirmation only")]
        public string? OrderName { get; init; }

        [CommandOption("--track")]
        [Description("Track your recent order")]
        public bool Track { get; init; }

        [CommandOption("--debug")]
        [Description("Display debug information")]
        public bool Debug { get; init; }

        public override ValidationResult Validate() =>
            DefaultOrder && OrderName is not null
                ? ValidationResult.Error("The --default-order and --order arguments are mutually exclusive.")
                : base.Validate();
    }

    private async Task<int> PizzaMain(bool defaultOrder, string? orderName, bool track, bool debug) {
        if (debug) {
            _debugInfo.Print();
            return 0;
        }

        if (defaultOrder) {
            var wasPlaced = await _controller.PlaceDefaultOrder();
            if (wasPlaced && track) await _controller.TrackOrder(TimeSpan.FromMinutes(2));
            return 0;
        }

        if (orderName is not null) {
            var wasPlaced = await _controller.PlaceOrder(orderName);
            if (wasPlaced && track) await _controller.TrackOrder(TimeSpan.FromMinutes(2));
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

sealed class DebugInfo {
    public required string OpenAiApiKey { get; init; }
    public required string DataRootDir  { get; init; }
    public required string ConfigRootDir  { get; init; }
    public required string? DotnetEnv  { get; init; }
    public required string? Editor  { get; init; }
    public required IPAddress IPAddress  { get; init; }
    public required int Port  { get; init; }

    public void Print() {
        Console.WriteLine($"""
            OpenAI API Key: {OpenAiApiKey}
            Data Root Dir: {DataRootDir}
            Config Root Dir: {ConfigRootDir}
            DOTNET_ENVIRONMENT: {DotnetEnv}
            EDITOR: {Editor}
            Server:
              IP Address: {IPAddress}
              Port: {Port}
            """);
    }
}
