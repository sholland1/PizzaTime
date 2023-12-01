using System.CommandLine;
using System.CommandLine.Parsing;
using System.Net;
using Controllers;
using Hollandsoft.OrderPizza;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using OpenAI.Extensions;
using Server;

return await BuildCommand().InvokeAsync(args);

async Task<int> PizzaMain(bool defaultOrder, string? orderName, bool track, string? apiKey) {
    if (apiKey is not null) {
        //Environment.SetEnvironmentVariable("OPENAI_API_KEY", apiKey);
        return 0;
    }

    using var provider = BuilderServiceProvider();

    try {
        var controller = provider.GetRequiredService<PizzaController>();
        if (defaultOrder) {
            await controller.PlaceDefaultOrder();
            return 0;
        }

        if (orderName is not null) {
            await controller.PlaceOrder(orderName);
            return 0;
        }

        if (track) {
            await controller.TrackOrder();
            return 0;
        }

        _ = provider.GetRequiredService<PizzaQueryServer>().StartServer();
        await controller.OpenProgram();
        return 0;
    }
    catch (Exception ex) {
        var logger = provider.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "Unhandled exception");
        LogManager.Shutdown();
        return 1;
    }
}

static ServiceProvider BuilderServiceProvider() {
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddUserSecrets<Program>()
        .AddEnvironmentVariables()
        .Build();

    ServiceCollection services = new();
    services.AddSingleton<IConfiguration>(configuration);
    services.AddSingleton(new HttpOptions(
        IPAddress.Parse(configuration.GetValue<string>("PizzaQueryServer:Host")!),
        configuration.GetValue<int>("PizzaQueryServer:Port")));

    services.AddOpenAIService();

    var dotnetEnv = configuration.GetValue<string>("DOTNET_ENVIRONMENT");
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
        .AddSingleton<TerminalSpinner>();

    var editor = configuration.GetValue<string>("EDITOR");
    services.AddSingleton<IEditor>(editor is not null
        ? new InstalledProgramEditor(editor, "AIPizzaPromptSystemMessage.txt")
        : new FallbackEditor("AIPizzaPromptSystemMessage.txt"));

    services
        .AddSingleton<PizzaController>()
        .AddSingleton<PizzaQueryServer>();

    return services.BuildServiceProvider();
}

RootCommand BuildCommand() {
    Option<bool> defaultOrderOption = new(
        new[] { "--default-order" },
        "Place the default order with user confirmation only");

    Option<string> orderOption = new(
        new[] { "--order" },
        "Place the order with the specified name with user confirmation only");

    Option<bool> trackOption = new(
        new[] { "--track" },
        "Track your recent order");

    Option<string> apiKeyOption = new(
        new[] { "--set-api-key" },
        "OpenAI API key");

    RootCommand rootCommand = new("Order a pizza") { defaultOrderOption, orderOption, trackOption, apiKeyOption };

    rootCommand.AddValidator(commandResult => {
        if (commandResult.Children.Count > 1) {
            commandResult.ErrorMessage = "All arguments are mutually exclusive.";
        }
    });

    rootCommand.SetHandler(PizzaMain, defaultOrderOption, orderOption, trackOption, apiKeyOption);
    return rootCommand;
}

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
