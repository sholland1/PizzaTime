using Hollandsoft.OrderPizza;

namespace Controllers;
public partial class PizzaController {
    private readonly IPizzaRepo _repo;
    private readonly Func<OrderInfo, ICart> _startOrder;
    private readonly ITerminalUI _terminalUI;
    private readonly IUserChooser _chooser;
    private readonly IAIPizzaBuilder _aiPizzaBuilder;

    public PizzaController(IPizzaRepo repo, Func<OrderInfo, ICart> startOrder, ITerminalUI terminalUI, IUserChooser chooser, IAIPizzaBuilder aiPizzaBuilder) =>
        (_repo, _startOrder, _terminalUI, _chooser, _aiPizzaBuilder) = (repo, startOrder, terminalUI, chooser, aiPizzaBuilder);

    public async Task FastPizza() {
        var userOrder = _repo.GetDefaultOrder()
            ?? throw new NotImplementedException("Implement create order.");

        var personalInfo = _repo.GetPersonalInfo() ?? CreatePersonalInfo();

        var userPayment = userOrder.PaymentType == PaymentType.PayAtStore
            ? Payment.PayAtStoreInstance
            : _repo.GetDefaultPayment() ?? CreatePayment();

        if (userPayment is null) {
            _terminalUI.PrintLine("No payment information found.");
            return;
        }

        var cart = _startOrder(userOrder.OrderInfo);

        bool firstTime = true;
        foreach (var pizza in userOrder.Pizzas) {
            var cartResult = await cart.AddPizza(pizza);
            cartResult.Match(
                _terminalUI.PrintLine,
                v => {
                    if (firstTime) {
                        firstTime = false;
                        _terminalUI.PrintLine($"Order ID: {v.OrderID}\n");
                    }

                    _terminalUI.PrintLine($"Pizza was added to cart. Product Count: {v.ProductCount}\n{pizza.Summarize()}\n");
                });
            if (cartResult.IsFailure) return;
        }

        foreach (var coupon in userOrder.Coupons) {
            cart.AddCoupon(coupon);
            _terminalUI.PrintLine($"Coupon {coupon.Code} was added to cart.");
        }
        if (userOrder.Coupons.Any()) {
            _terminalUI.PrintLine();
        }

        var priceResult = await cart.GetSummary();
        priceResult.Match(
            message => _terminalUI.PrintLine($"Failed to check cart price:\n{message}"),
            summary => _terminalUI.PrintLine(
                $"""
                Cart summary:
                {userOrder.OrderInfo.Summarize()}
                Estimated Wait: {summary.WaitTime}
                Price: ${summary.TotalPrice}

                {userPayment.Summarize()}

                """));
        if (priceResult.IsFailure) return;

        var answer = _terminalUI.Prompt("Confirm order? [Y/n]: ");
        _terminalUI.PrintLine();

        if (!IsAffirmative(answer)) {
            _terminalUI.PrintLine("Order cancelled.");
            return;
        }

        _terminalUI.PrintLine("Ordering pizza...");

        var orderResult = await cart.PlaceOrder(personalInfo, userPayment);
        _terminalUI.PrintLine(
            orderResult.Match(
                message => $"Failed to place order: {message}",
                message => $"Order summary:\n{message}\nDone."));
    }

    public async Task ShowOptions() {
        _terminalUI.PrintLine("Welcome to the pizza ordering app!🍕");
        await Helper();

        async Task Helper() {
            string[] options = {
                "1. Order default",
                "2. Start new order",
                "3. Manage pizzas",
                "4. Manage personal info",
                "5. Manage payments",
                "6. Track order",
                "q. Exit"
            };
            _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
            var choice = _terminalUI.PromptKey("Choose an option: ");

            switch (choice) {
                case '1': await FastPizza(); break;
                // case '2': await NewOrder(); break;
                case '3': await ManagePizzas(); await Helper(); break;
                case '4': _ = ManagePersonalInfo(); await Helper(); break;
                case '5': _ = ManagePayments(); await Helper(); break;
                // case '6': await TrackOrder(); await Helper(); break;
                case 'Q' or 'q': _terminalUI.PrintLine("Goodbye!"); return;
                default:
                    _terminalUI.PrintLine("Not a valid option. Try again.");
                    await Helper();
                    break;
            }
        }
    }

    private static bool IsAffirmative(string? answer) => (answer?.ToLower() ?? "y") == "y";
}
