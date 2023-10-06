using Microsoft.Extensions.Logging.Abstractions;

AppDomain.CurrentDomain.UnhandledException += (_, args) => {
    var ex = (Exception)args.ExceptionObject;
    Console.Error.WriteLine($"Unhandled exception: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    Environment.Exit(1);
};

PizzaRepository repo = new();
DominosApi api = new(new NullLogger<DominosApi>());
var orderInfo = repo.GetOrderInfo("defaultOrderInfo");
DominosCart cart = new(api, orderInfo);

var pizza1 = repo.GetPizza("defaultPizza");
var result1 = await cart.AddPizza(pizza1);
Console.WriteLine(result1.Message);

var p2 = repo.GetPizza("OtherPizza");
var result2 = await cart.AddPizza(p2);
Console.WriteLine(result2.Message);

var result3 = await cart.GetSummary();
Console.WriteLine(result3.Message);

var payment = repo.GetPaymentInfo("defaultPaymentInfo");
var result4 = await cart.PlaceOrder(payment);
Console.WriteLine(result4.Message);
