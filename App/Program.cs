AppDomain.CurrentDomain.UnhandledException += (_, args) => {
    var ex = (Exception)args.ExceptionObject;
    Console.Error.WriteLine($"Unhandled exception: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    Environment.Exit(1);
};

PizzaRepository repo = new();
DominosApi api = new();
DominosCart cart = new(config, api);

var p1 = repo.GetPizza("defaultPizza");
var result1 = await cart.AddPizza(p1);
Console.WriteLine(result1.Message);

var p2 = repo.GetPizza("OtherPizza");
var result2 = await cart.AddPizza(p2);
Console.WriteLine(result2.Message);

var result3 = await cart.GetSummary();
Console.WriteLine(result3.Message);