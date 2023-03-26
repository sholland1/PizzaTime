AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
    var ex = (Exception)args.ExceptionObject;
    Console.Error.WriteLine($"Unhandled exception: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    Environment.Exit(1);
};

PizzaRepository repo = new();
DominosConfig config = new() { StoreID = 1000 };
DominosApi api = new(config);

var p1 = repo.GetPizza("defaultPizza");
var result1 = await api.AddPizzaToCart(p1);
Console.WriteLine(result1.Message);

var p2 = repo.GetPizza("OtherPizza");
var result2 = await api.AddPizzaToCart(p2);
Console.WriteLine(result2.Message);