using System.CommandLine;
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

using var provider = BuilderServiceProvider();

return await BuildCommand().InvokeAsync(args);

async Task<int> PizzaMain(bool defaultOrder, string? orderName) {
    if (defaultOrder && orderName is not null) {
        Console.WriteLine("Cannot specify both --defaultOrder and --order");
        return 2;
    }

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
        new[] { "--defaultOrder" },
        "Place the default order with user confirmation only");

    Option<string> orderOption = new(
        new[] { "--order" },
        "Place the order with the specified name with user confirmation only");

    RootCommand rootCommand = new("Order a pizza") { defaultOrderOption, orderOption };
    rootCommand.SetHandler(PizzaMain, defaultOrderOption, orderOption);
    return rootCommand;
}
