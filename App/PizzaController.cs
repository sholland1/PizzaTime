using Hollandsoft.OrderPizza;

namespace Controllers;
public partial class PizzaController(
    IPizzaRepo _repo,
    Func<OrderInfo, ICart> _startOrder,
    IAIPizzaBuilder _aiPizzaBuilder,
    IStoreApi _storeApi,
    ITerminalUI _terminalUI,
    IUserChooser _chooser,
    TerminalSpinner _spinner,
    IEditor _editor,
    IDateGetter _dateGetter) {

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

        var (orderName, order) = userOrder;
        await OrderPizza(orderName, order, personalInfo);
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

        await OrderPizza(orderName, order, personalInfo);
    }

    public async Task OrderPizza(string orderName, ActualOrder userOrder, PersonalInfo personalInfo) {
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
        if (userOrder.Coupons.Count != 0) {
            _terminalUI.PrintLine();
        }

        var priceResult = await cart.GetSummary();
        var summarySuccess = priceResult.Match(
            message => {
                _terminalUI.PrintLine($"Failed to check cart price:\n{message}");
                return default(SummarySuccess?);
            },
            summary => {
                _terminalUI.PrintLine($"""
                Cart summary:
                {userOrder.OrderInfo.Summarize()}
                Estimated Wait: {summary.WaitTime}
                Price: ${summary.TotalPrice}

                {userOrder.Payment.Summarize()}

                """);
                return summary;
            });
        if (summarySuccess is null) return;

        var answer = _terminalUI.Prompt("Confirm order? [Y/n]: ");
        _terminalUI.PrintLine();

        if (!IsAffirmative(answer)) {
            _terminalUI.PrintLine("Order cancelled.");
            return;
        }

        _terminalUI.PrintLine("Ordering pizza...");

        var orderResult = await cart.PlaceOrder(personalInfo, userOrder.Payment);

        orderResult.Match(
            message => _terminalUI.PrintLine($"Failed to place order: {message}"),
            message => {
                PastOrder pastOrder = new() {
                    OrderName = orderName,
                    TimeStamp = _dateGetter.GetDateTime().TruncateToSeconds(),
                    Order = userOrder.ToHistOrder(),
                    EstimatedWaitMinutes = summarySuccess.WaitTime,
                    TotalPrice = summarySuccess.TotalPrice
                };
                _repo.AddOrderToHistory(pastOrder);
                _terminalUI.PrintLine($"Order summary:\n{message}\nDone.");
            });
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
        await OrderPizza(orderName, order, _repo.GetPersonalInfo()!);
    }

    public async Task OpenProgram() {
        _terminalUI.Clear();
        await MainMenu();
    }

    public async Task MainMenu() {
        _terminalUI.PrintLine("Welcome to the pizza ordering app!🍕");

        string[] options = [
            "1. Place order",
            "2. Manage orders",
            "3. Manage pizzas",
            "4. Manage payments",
            "5. Edit personal info",
            "6. Track order",
            "7. View order history",
            "q. Exit"
        ];
        _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = _terminalUI.PromptKey("Choose an option: ");

        if (choice is 'Q' or 'q') {
            _terminalUI.PrintLine("Goodbye!");
            return;
        }

        _terminalUI.Clear();

        switch (choice) {
            case '1': await PlaceOrder(); break;
            case '2': await ManageOrdersMenu(); break;
            case '3': await ManagePizzasMenu(); break;
            case '4': _ = ManagePaymentsMenu(); break;
            case '5': _ = ManagePersonalInfo(); break;
            // case '6': await TrackOrder(); break;
            case '7': ViewPastOrders(); break;
            default:
                _terminalUI.PrintLine("Not a valid option. Try again.");
                break;
        }

        await MainMenu();
    }

    private void ViewPastOrders() {
        var orders = _repo.ListPastOrders().ToList();
        var message = $"You have placed {orders.Count} orders with this program.";
        _chooser.IgnoreUserChoice(message, orders.Select(o => o.ToString()), "pastorder");
    }

    public Task TrackOrder() {
        throw new NotImplementedException();
    }

    private static bool IsAffirmative(string? answer) => answer is null or "Y" or "y";
}
