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
            ? PaymentInfo.PayAtStoreInstance
            : _repo.GetDefaultPaymentInfo()
                ?? throw new InvalidOperationException("Implement create payment info");

        var cart = _startOrder(userOrder.OrderInfo);

        bool firstTime = true;
        foreach (var pizza in userOrder.Pizzas) {
            var cartResult = await cart.AddPizza(pizza);
            if (!cartResult.Success) {
                _consoleUI.PrintLine(cartResult.Message);
                return;
            }

            if (firstTime) {
                firstTime = false;
                _consoleUI.PrintLine($"Order ID: {cartResult.OrderID}\n");
            }

            _consoleUI.PrintLine($"{cartResult.Message} Product Count: {cartResult.ProductCount}\n{pizza.Summarize()}\n");
        }

        foreach (var coupon in userOrder.Coupons) {
            cart.AddCoupon(coupon);
            _consoleUI.PrintLine($"Coupon {coupon.Code} was added to cart.");
        }
        if (userOrder.Coupons.Any()) {
            _consoleUI.PrintLine();
        }

        var priceResult = await cart.GetSummary();
        if (!priceResult.Success) {
            _consoleUI.PrintLine($"Failed to check cart price:\n{priceResult.Message}");
            return;
        }

        _consoleUI.PrintLine(
            $"""
            Cart summary:
            {userOrder.OrderInfo.Summarize()}
            Estimated Wait: {priceResult.WaitTime}
            Price: ${priceResult.TotalPrice}

            {userPayment.Summarize()}

            """);

        var answer = _consoleUI.Prompt("Confirm order? [Y/n]: ");
        _consoleUI.PrintLine();

        if (!IsAffirmative(answer)) {
            _consoleUI.PrintLine("Order cancelled.");
            return;
        }

        _consoleUI.PrintLine("Ordering pizza...");

        var orderResult = await cart.PlaceOrder(personalInfo, userPayment);
        if (!orderResult.Success) {
            _consoleUI.PrintLine($"Failed to place order: {orderResult.Message}");
            return;
        }

        _consoleUI.PrintLine($"Order summary:\n{orderResult.Message}");
        _consoleUI.PrintLine("Done.");
    }

    private static bool IsAffirmative(string? answer) => (answer?.ToLower() ?? "y") == "y";
}
