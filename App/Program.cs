using Controllers;
using Hollandsoft.OrderPizza;
using Microsoft.Extensions.Logging.Abstractions;

AppDomain.CurrentDomain.UnhandledException += (_, args) => {
    var ex = (Exception)args.ExceptionObject;
    Console.Error.WriteLine($"Unhandled exception: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    Environment.Exit(1);
};

var fastFlag = args.Contains("--fast");

var pizzaController = new PizzaController(
    new PizzaRepository(),
    o => new DominosCart(
        new DominosApi(new NullLogger<DominosApi>()),
        o),
    new RealConsoleUI());

if (fastFlag) {
    await pizzaController.FastPizza();
    return;
}

await pizzaController.ShowOptions();
