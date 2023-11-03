using Hollandsoft.OrderPizza;

namespace Controllers;
public class PizzaController {
    private readonly IPizzaRepo _repo;
    private readonly Func<OrderInfo, ICart> _startOrder;
    private readonly IConsoleUI _consoleUI;

    public PizzaController(IPizzaRepo repo, Func<OrderInfo, ICart> startOrder, IConsoleUI consoleUI) =>
        (_repo, _startOrder, _consoleUI) = (repo, startOrder, consoleUI);

    public async Task FastPizza() {
        var userOrder = _repo.GetDefaultOrder()
            ?? throw new InvalidOperationException("Implement create order.");

        var personalInfo = _repo.GetPersonalInfo()
            ?? throw new InvalidOperationException("Implement create personal info");

        var userPayment = userOrder.PaymentType == PaymentType.PayAtStore
            ?  Payment.PayAtStoreInstance
            : _repo.GetDefaultPayment()
                ?? throw new InvalidOperationException("Implement create payment info");

        var cart = _startOrder(userOrder.OrderInfo);

        bool firstTime = true;
        foreach (var pizza in userOrder.Pizzas) {
            var cartResult = await cart.AddPizza(pizza);
            cartResult.Match(
                _consoleUI.PrintLine,
                v => {
                    if (firstTime) {
                        firstTime = false;
                        _consoleUI.PrintLine($"Order ID: {v.OrderID}\n");
                    }

                    _consoleUI.PrintLine($"Pizza was added to cart. Product Count: {v.ProductCount}\n{pizza.Summarize()}\n");
                });
            if (cartResult.IsFailure) return;
        }

        foreach (var coupon in userOrder.Coupons) {
            cart.AddCoupon(coupon);
            _consoleUI.PrintLine($"Coupon {coupon.Code} was added to cart.");
        }
        if (userOrder.Coupons.Any()) {
            _consoleUI.PrintLine();
        }

        var priceResult = await cart.GetSummary();
        priceResult.Match(
            message => _consoleUI.PrintLine($"Failed to check cart price:\n{message}"),
            summary => _consoleUI.PrintLine(
                $"""
                Cart summary:
                {userOrder.OrderInfo.Summarize()}
                Estimated Wait: {summary.WaitTime}
                Price: ${summary.TotalPrice}

                {userPayment.Summarize()}

                """));
        if (priceResult.IsFailure) return;

        var answer = _consoleUI.Prompt("Confirm order? [Y/n]: ");
        _consoleUI.PrintLine();

        if (!IsAffirmative(answer)) {
            _consoleUI.PrintLine("Order cancelled.");
            return;
        }

        _consoleUI.PrintLine("Ordering pizza...");

        var orderResult = await cart.PlaceOrder(personalInfo, userPayment);
        _consoleUI.PrintLine(
            orderResult.Match(
                message => $"Failed to place order: {message}",
                message => $"Order summary:\n{message}\nDone."));
    }

    public async Task ShowOptions() {
        _consoleUI.PrintLine("Welcome to the pizza ordering app!🍕");
        await Helper();

        async Task Helper() {
            string[] options = {
                "1. Order default",
                "2. Start new order",
                "3. Edit saved pizzas",
                "4. Edit personal info",
                "5. Track order",
                "q. Exit"
            };
            _consoleUI.PrintLine(string.Join('\n', options));
            var choice = _consoleUI.PromptKey("Choose an option: ");
            // return _consoleUI.FuzzyChoice(options);

            switch (choice) {
                case '1': await FastPizza(); break;
                // case '2': await NewOrder(); break;
                // case '3': await EditSavedPizzas(); break;
                // case '4': await EditPersonalInfo(); break;
                // case '5': await EditPaymentInfo(); break;
                case 'Q' or 'q': _consoleUI.PrintLine("Goodbye!"); return;
                default:
                    _consoleUI.PrintLine("Not a valid option. Try again.");
                    await Helper();
                    break;
            }
        }
    }

    private static bool IsAffirmative(string? answer) => (answer?.ToLower() ?? "y") == "y";
}
