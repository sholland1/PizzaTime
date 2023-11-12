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

    public async Task PlaceDefaultOrder() {
        var userOrder = _repo.GetDefaultOrder();
        if (userOrder is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No default order found.");
            return;
        }

        var personalInfo = _repo.GetPersonalInfo();
        if (personalInfo is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No personal information found.");
            return;
        }

        await OrderPizza(userOrder, personalInfo);
    }

    public async Task PlaceOrder(string orderName) {
        var order = _repo.GetOrder(orderName);
        if (order is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("Order not found.");
            return;
        }

        var personalInfo = _repo.GetPersonalInfo();
        if (personalInfo is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No personal information found.");
            return;
        }

        await OrderPizza(order, personalInfo);
    }

    public async Task OrderPizza(ActualOrder userOrder, PersonalInfo personalInfo) {
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

                {userOrder.Payment.Summarize()}

                """));
        if (priceResult.IsFailure) return;

        var answer = _terminalUI.Prompt("Confirm order? [Y/n]: ");
        _terminalUI.PrintLine();

        if (!IsAffirmative(answer)) {
            _terminalUI.PrintLine("Order cancelled.");
            return;
        }

        _terminalUI.PrintLine("Ordering pizza...");

        var orderResult = await cart.PlaceOrder(personalInfo, userOrder.Payment);
        _terminalUI.PrintLine(
            orderResult.Match(
                message => $"Failed to place order: {message}",
                message => $"Order summary:\n{message}\nDone."));
    }

    private async Task PlaceOrder() {
        var orderName = _chooser.GetUserChoice(
            "Choose an order to place: ", _repo.ListOrders(), "order");
        if (orderName is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No order selected.");
            return;
        }

        var order = _repo.GetOrder(orderName) ?? throw new Exception("Order not found.");

        var personalInfo = _repo.GetPersonalInfo();
        if (personalInfo is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No personal information found.");
            return;
        }

        _terminalUI.PrintLine($"Placing '{orderName}' order:");
        await OrderPizza(order, _repo.GetPersonalInfo()!);
    }

    public async Task OpenProgram() {
        _terminalUI.Clear();
        await MainMenu();
    }

    public async Task MainMenu() {
        _terminalUI.PrintLine("Welcome to the pizza ordering app!🍕");

        string[] options = {
            "1. Place order",
            "2. Manage orders",
            "3. Manage pizzas",
            "4. Manage payments",
            "5. Edit personal info",
            "6. Track order",
            "q. Exit"
        };
        _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = _terminalUI.PromptKey("Choose an option: ");

        switch (choice) {
            case '1': _terminalUI.Clear(); await PlaceOrder(); await MainMenu(); break;
            case '2': _terminalUI.Clear(); await ManageOrdersMenu(); await MainMenu(); break;
            case '3': _terminalUI.Clear(); await ManagePizzasMenu(); await MainMenu(); break;
            case '4': _terminalUI.Clear(); _ = ManagePaymentsMenu(); await MainMenu(); break;
            case '5': _terminalUI.Clear(); _ = ManagePersonalInfo(); await MainMenu(); break;
            // case '6': _terminalUI.Clear(); await TrackOrder(); await MainMenu(); break;
            case 'Q' or 'q': _terminalUI.PrintLine("Goodbye!"); return;
            default:
                _terminalUI.Clear();
                _terminalUI.PrintLine("Not a valid option. Try again.");
                await MainMenu();
                break;
        }
    }

    private static bool IsAffirmative(string? answer) => (answer?.ToLower() ?? "y") == "y";
}
