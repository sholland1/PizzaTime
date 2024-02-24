using System.Diagnostics;
using Hollandsoft.PizzaTime;

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

    public async Task<bool> PlaceDefaultOrder() {
        var userOrder = _repo.GetDefaultOrder();
        if (userOrder is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No default order found.");
            return false;
        }

        var personalInfo = _repo.GetPersonalInfo();
        if (personalInfo is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No personal information found.");
            return false;
        }

        var (orderName, order) = userOrder;
        return await OrderPizza(orderName, order, personalInfo);
    }

    public async Task<bool> PlaceOrder(string orderName) {
        var order = _repo.GetOrder(orderName);
        if (order is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("Order not found.");
            return false;
        }

        var personalInfo = _repo.GetPersonalInfo();
        if (personalInfo is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No personal information found.");
            return false;
        }

        return await OrderPizza(orderName, order, personalInfo);
    }

    public async Task<bool> OrderPizza(string orderName, ActualOrder userOrder, PersonalInfo personalInfo) {
        _terminalUI.PrintLine($"Starting order: '{orderName}'");
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
            if (cartResult.IsFailure) return false;
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
        if (summarySuccess is null) return false;

        var answer = _terminalUI.Prompt("Confirm order? [Y/n]: ");
        _terminalUI.PrintLine();

        if (!IsAffirmative(answer)) {
            _terminalUI.PrintLine("Order cancelled.");
            return false;
        }

        _terminalUI.PrintLine("Ordering pizza...");

        var orderResult = await cart.PlaceOrder(personalInfo, userOrder.Payment);

        return orderResult.Match(
            message => {
                _terminalUI.PrintLine($"Failed to place order: {message}");
                return false;
            },
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
                return true;
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

        var order = _repo.GetOrder(orderName);
        Debug.Assert(order is not null, "Order not found.");

        var personalInfo = _repo.GetPersonalInfo();
        if (personalInfo is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No personal information found.");
            return;
        }

        _terminalUI.PrintLine($"Placing '{orderName}' order:");
        _ = await OrderPizza(orderName, order, _repo.GetPersonalInfo()!);
    }

    public async Task OpenProgram() {
        _terminalUI.Clear();
        await MainMenu();
    }

    public async Task MainMenu() {
        _terminalUI.PrintLine("It's Pizza Time!🍕");

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
            case '6': await TrackOrder(); break;
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

    public async Task TrackOrder(TimeSpan? totalWaitTime = null) {
        totalWaitTime ??= TimeSpan.Zero;

        var phone = _repo.GetPersonalInfo()?.Phone;
        if (phone is null) {
            _terminalUI.PrintLine("No phone number found.");
            return;
        }

        var orders = await _spinner.Show(
            "Searching for orders...",
            async () => await SearchForOrders(phone, totalWaitTime.Value));

        _terminalUI.Clear();

        if (orders.Length == 0) {
            _terminalUI.PrintLine("No orders found.");
            return;
        }

        var order = SelectOrderToTrack(orders);

        _terminalUI.PrintLine($"Tracking order {order.OrderID}...");
        var request = order.ToTrackRequest();
        var timeToSleep = TimeSpan.FromSeconds(30);

        while (true) {
            var trackResult = await _storeApi.TrackOrder(request);

            _terminalUI.Print($"Status: {trackResult.OrderStatus}, ");
            if (trackResult.RackTime is not null) {
                _terminalUI.PrintLine($"Rack Time: {trackResult.RackTime}");
                break;
            }
            else if (trackResult.OvenTime is not null)
                _terminalUI.PrintLine($"Oven Time: {trackResult.OvenTime}");
            else if (trackResult.StartTime is not null)
                _terminalUI.PrintLine($"Start Time: {trackResult.StartTime}");

            await Task.Delay(timeToSleep);
        }

        _terminalUI.PrintLine($"Order is ready! {_dateGetter.GetDateTime():T}");
    }

    private async Task<InitialTrackResponse[]> SearchForOrders(string phone, TimeSpan totalWaitTime) {
        var waitInterval = TimeSpan.FromSeconds(2);
        var timer = Stopwatch.StartNew();
        while (true) {
            var orders = await _storeApi.InitiateTrackOrder(new(phone));
            if (orders.Length != 0 || timer.Elapsed > totalWaitTime) return orders;
            await Task.Delay(waitInterval);
        }
    }

    private InitialTrackResponse SelectOrderToTrack(InitialTrackResponse[] orders) {
        if (orders.Length == 1) return orders[0];

        var orderID = _chooser.GetUserChoice(
            "Multiple orders found. Choose an order to track: ",
            orders.Select(o => o.OrderID));
        return orders.First(o => o.OrderID == orderID);
    }

    private static bool IsAffirmative(string? answer) => answer is null or "Y" or "y";
}
