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

async Task PizzaMain(bool defaultOrder, string? orderName) {
    if (defaultOrder && orderName is not null) {
        Console.WriteLine("Cannot specify both --defaultOrder and --order");
        return;
    }

    var controller = provider.GetRequiredService<PizzaController>();
    if (defaultOrder) {
        await controller.PlaceDefaultOrder();
        return;
    }

    if (orderName is not null) {
        await controller.PlaceOrder(orderName);
        return;
    }

    _ = provider.GetRequiredService<PizzaQueryServer>().StartServer();
    await controller.OpenProgram();
}

static ServiceProvider BuilderServiceProvider() {
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddUserSecrets<Program>()
        .Build();

    ServiceCollection services = new();
    services.AddSingleton<IConfiguration>(configuration);
    services.AddSingleton(new HttpOptions(
        IPAddress.Parse(configuration.GetValue<string>("PizzaQueryServer:Host")!),
        configuration.GetValue<int>("PizzaQueryServer:Port")));

    services.AddOpenAIService();

    services.AddLogging()
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
        .AddSingleton<PizzaController>();

    services.AddSingleton<PizzaQueryServer>();

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
