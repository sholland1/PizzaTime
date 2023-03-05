public class PizzaController {
    private readonly IPizzaRepo _repo;
    private readonly IPizzaApi _api;
    private readonly IConsoleUI _consoleUI;

    public PizzaController(IPizzaRepo repo, IPizzaApi api, IConsoleUI consoleUI) =>
        (_repo, _api, _consoleUI) = (repo, api, consoleUI);

    public void FastPizza() {
        var userPizza = _repo.GetPizza("defaultPizza");
        var userOrder = _repo.GetOrderInfo("defaultOrderInfo");
        var userPayment = _repo.GetPaymentInfo("defaultPaymentInfo");

        var cartResult = _api.AddPizzaToCart(userPizza);
        if (!cartResult.Success) {
            _consoleUI.PrintLine($"Pizza was not added to cart: {cartResult.Message}");
            return;
        }

        _consoleUI.PrintLine($"Pizza was added to cart:\n{cartResult.Summarize()}\n");

        var priceResult = _api.CheckCartTotal();
        if (!priceResult.Success) {
            _consoleUI.PrintLine($"Failed to check cart price:\n{priceResult.Message}");
            return;
        }

        _consoleUI.PrintLine($"Cart summary:\n{priceResult.Summarize()}\n");

        var answer = _consoleUI.Prompt("Confirm order? [Y/n]: ") ?? "y";
        _consoleUI.PrintLine();

        if (IsAffirmative(answer)) {
            _consoleUI.PrintLine("Order cancelled.");
            return;
        }

        _consoleUI.PrintLine("Ordering pizza...");

        var orderResult = _api.OrderPizza(userOrder, userPayment);
        if (!orderResult.Success) {
            _consoleUI.PrintLine($"Failed to place order: {orderResult.Message}");
            return;
        }

        _consoleUI.PrintLine($"Order summary:\n{orderResult.Summarize()}");
        _consoleUI.PrintLine("Done.");
    }

    private static bool IsAffirmative(string answer) => answer.ToLower() != "y";
}