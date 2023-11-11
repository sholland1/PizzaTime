using System.CommandLine;
using System.Net;
using Controllers;
using Hollandsoft.OrderPizza;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI.Extensions;
using Server;

var provider = BuilderServiceProvider();

AppDomain.CurrentDomain.UnhandledException += (_, args) => {
    var ex = (Exception)args.ExceptionObject;
    provider.GetRequiredService<ILogger<Program>>()
        .LogCritical(ex, "Unhandled exception");
    Environment.Exit(1);
};

await BuildCommand().InvokeAsync(args);

async Task PizzaMain(bool fast) {
    var controller = provider.GetRequiredService<PizzaController>();
    if (fast) {
        await controller.FastPizza();
        return;
    }
    _ = provider.GetRequiredService<PizzaQueryServer>().StartServer();
    await controller.ShowOptions();
}

static ServiceProvider BuilderServiceProvider() {
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddUserSecrets<Program>()
        .Build();

    ServiceCollection services = new();
    services.AddSingleton<IConfiguration>(configuration);

    services.AddOpenAIService();

    services.AddLogging()
        .AddSingleton<ISerializer>(MyJsonSerializer.Instance)
        .AddSingleton<IOrderApi, DominosApi>()
        .AddSingleton<IPizzaRepo, PizzaRepository>()
        .AddSingleton<Func<OrderInfo, ICart>>(services =>
        o => new DominosCart(
            services.GetRequiredService<IOrderApi>()!, o))

        .AddSingleton(new AIPizzaBuilderConfig {
            SystemMessageFile = "AIPizzaPromptSystemMessage.txt",
            FewShotFile = "FewShotPrompt.json"
        })
        .AddSingleton<IAIPizzaBuilder, ChatCompletionsPizzaBuilder>()

        .AddSingleton<ITerminalUI, RealTerminalUI>()
        .AddSingleton<PizzaController>();

    services.AddSingleton<PizzaQueryServer>(services =>
        new(
            IPAddress.Parse(configuration.GetValue<string>("PizzaQueryServer:Host")!),
            configuration.GetValue<int>("PizzaQueryServer:Port"),
            services.GetRequiredService<IPizzaRepo>()));

    return services.BuildServiceProvider();
}

RootCommand BuildCommand() {
    Option<bool> fastOption = new(
        new[] { "--fast" },
        "Order the default pizza with user confirmation only");

    RootCommand rootCommand = new("Order a pizza") { fastOption };
    rootCommand.SetHandler(PizzaMain, fastOption);
    return rootCommand;
}
