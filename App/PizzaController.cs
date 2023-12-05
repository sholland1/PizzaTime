using Hollandsoft.OrderPizza;

namespace Controllers;
public partial class PizzaController(
    IPizzaRepo Repo,
    Func<OrderInfo, ICart> StartOrder,
    IAIPizzaBuilder AiPizzaBuilder,
    IStoreApi StoreApi,
    ITerminalUI TerminalUI,
    IUserChooser Chooser,
    TerminalSpinner Spinner,
    IEditor Editor) {

    public async Task PlaceDefaultOrder() {
        var userOrder = Repo.GetDefaultOrder();
        if (userOrder is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No default order found.");
            return;
        }

        var personalInfo = Repo.GetPersonalInfo();
        if (personalInfo is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No personal information found.");
            return;
        }

        await OrderPizza(userOrder, personalInfo);
    }

    public async Task PlaceOrder(string orderName) {
        var order = Repo.GetOrder(orderName);
        if (order is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("Order not found.");
            return;
        }

        var personalInfo = Repo.GetPersonalInfo();
        if (personalInfo is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No personal information found.");
            return;
        }

        await OrderPizza(order, personalInfo);
    }

    public async Task OrderPizza(ActualOrder userOrder, PersonalInfo personalInfo) {
        var cart = StartOrder(userOrder.OrderInfo);

        bool firstTime = true;
        foreach (var pizza in userOrder.Pizzas) {
            var cartResult = await cart.AddPizza(pizza);
            cartResult.Match(
                TerminalUI.PrintLine,
                v => {
                    if (firstTime) {
                        firstTime = false;
                        TerminalUI.PrintLine($"Order ID: {v.OrderID}\n");
                    }

                    TerminalUI.PrintLine($"Pizza was added to cart. Product Count: {v.ProductCount}\n{pizza.Summarize()}\n");
                });
            if (cartResult.IsFailure) return;
        }

        foreach (var coupon in userOrder.Coupons) {
            cart.AddCoupon(coupon);
            TerminalUI.PrintLine($"Coupon {coupon.Code} was added to cart.");
        }
        if (userOrder.Coupons.Count != 0) {
            TerminalUI.PrintLine();
        }

        var priceResult = await cart.GetSummary();
        priceResult.Match(
            message => TerminalUI.PrintLine($"Failed to check cart price:\n{message}"),
            summary => TerminalUI.PrintLine(
                $"""
                Cart summary:
                {userOrder.OrderInfo.Summarize()}
                Estimated Wait: {summary.WaitTime}
                Price: ${summary.TotalPrice}

                {userOrder.Payment.Summarize()}

                """));
        if (priceResult.IsFailure) return;

        var answer = TerminalUI.Prompt("Confirm order? [Y/n]: ");
        TerminalUI.PrintLine();

        if (!IsAffirmative(answer)) {
            TerminalUI.PrintLine("Order cancelled.");
            return;
        }

        TerminalUI.PrintLine("Ordering pizza...");

        //TODO: log order
        var orderResult = await cart.PlaceOrder(personalInfo, userOrder.Payment);
        TerminalUI.PrintLine(
            orderResult.Match(
                message => $"Failed to place order: {message}",
                message => $"Order summary:\n{message}\nDone."));
    }

    private async Task PlaceOrder() {
        var orderName = Chooser.GetUserChoice(
            "Choose an order to place: ", Repo.ListOrders(), "order");
        if (orderName is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No order selected.");
            return;
        }

        var order = Repo.GetOrder(orderName) ?? throw new Exception("Order not found.");

        var personalInfo = Repo.GetPersonalInfo();
        if (personalInfo is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No personal information found.");
            return;
        }

        TerminalUI.PrintLine($"Placing '{orderName}' order:");
        await OrderPizza(order, Repo.GetPersonalInfo()!);
    }

    public async Task OpenProgram() {
        TerminalUI.Clear();
        await MainMenu();
    }

    public async Task MainMenu() {
        TerminalUI.PrintLine("Welcome to the pizza ordering app!🍕");

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
        TerminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = TerminalUI.PromptKey("Choose an option: ");

        if (choice is 'Q' or 'q') {
            TerminalUI.PrintLine("Goodbye!");
            return;
        }

        TerminalUI.Clear(); 

        switch (choice) {
            case '1': await PlaceOrder(); break;
            case '2': await ManageOrdersMenu(); break;
            case '3': await ManagePizzasMenu(); break;
            case '4': _ = ManagePaymentsMenu(); break;
            case '5': _ = ManagePersonalInfo(); break;
            // case '6': await TrackOrder(); break;
            // case '7': ViewOrderHistory(); break;
            default:
                TerminalUI.PrintLine("Not a valid option. Try again.");
                break;
        }

        await MainMenu();
    }

    public Task TrackOrder() {
        throw new NotImplementedException();
    }

    private static bool IsAffirmative(string? answer) => answer is null or "Y" or "y";
}
