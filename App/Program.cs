AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
    var ex = (Exception)args.ExceptionObject;
    Console.Error.WriteLine($"Unhandled exception: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    Environment.Exit(1);
};

var pizzaController = new PizzaController(
    new PizzaRepository(),
    new DominosApi(new object()),
    new RealConsoleUI());
pizzaController.FastPizza();
