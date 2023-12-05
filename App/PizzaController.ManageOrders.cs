using Hollandsoft.OrderPizza;

namespace Controllers;
public partial class PizzaController {
    public async Task CreateOrdersMenu() {
        TerminalUI.PrintLine("--Manage Orders--");

        string[] options = [
            "1. Create new order",
            "q. Return"
        ];

        TerminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = TerminalUI.PromptKey("Choose an option: ");
        TerminalUI.Clear();

        switch (choice) {
            case '1': _ = CreateOrder(); await ManageOrdersMenu(); break;
            case 'Q' or 'q': return;
            default:
                TerminalUI.PrintLine("Not a valid option. Try again.");
                await CreateOrdersMenu();
                break;
        }
    }

    public async Task ManageOrdersMenu() {
        if (!Repo.ListOrders().Any()) {
            await CreateOrdersMenu();
            return;
        }

        TerminalUI.PrintLine("--Manage Orders--");

        string[] options = [
            "1. Set default order",
            "2. Create new order",
            "3. Edit existing order",
            "4. Delete existing order",
            "5. Rename existing order",
            "q. Return"
        ];
        TerminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = TerminalUI.PromptKey("Choose an option: ");
        TerminalUI.Clear();

        switch (choice) {
            case '1': SetDefaultOrder(); await ManageOrdersMenu(); break;
            case '2': await CreateOrder(); await ManageOrdersMenu(); break;
            case '3': await EditOrder(); await ManageOrdersMenu(); break;
            case '4': DeleteOrder(); await ManageOrdersMenu(); break;
            case '5': RenameOrder(); await ManageOrdersMenu(); break;
            case 'Q' or 'q': return;
            default:
                TerminalUI.PrintLine("Not a valid option. Try again.");
                await ManageOrdersMenu();
                break;
        }
    }

    private void RenameOrder() {
        var orderName = Chooser.GetUserChoice(
            "Choose an order to rename: ", Repo.ListOrders(), "order");
        if (orderName is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No order selected.");
            return;
        }

        var newOrderName = GetOrderName(orderName);
        if (orderName == newOrderName) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("Order not renamed.");
            return;
        }
        Repo.RenameOrder(orderName, newOrderName);

        TerminalUI.Clear();
        TerminalUI.PrintLine($"Order renamed to '{newOrderName}'.");
    }

    private void SetDefaultOrder() {
        var orderName = Chooser.GetUserChoice(
            "Choose an order to set as default: ", Repo.ListOrders(), "order");
        TerminalUI.Clear();
        if (orderName is null) {
            TerminalUI.PrintLine("No order selected.");
            return;
        }

        Repo.SetDefaultOrder(orderName);
        TerminalUI.PrintLine($"Default order set to '{orderName}'.");
    }

    private async Task EditOrder() {
        var orderName = Chooser.GetUserChoice(
            "Choose an order to edit: ", Repo.ListOrders(), "order");
        if (orderName is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No order selected.");
            return;
        }

        var order = Repo.GetSavedOrder(orderName) ?? throw new Exception("Order not found.");
        await EditOrder(orderName, order);
    }

    private async Task EditOrder(string orderName, SavedOrder order) {
        var actualOrder = Repo.GetActualFromSavedOrder(order);
        TerminalUI.WriteInfoPanel(50, actualOrder.Summarize().Split('\n'));
        TerminalUI.SetCursorPosition(0, 0);

        TerminalUI.PrintLine($"--Editing order '{orderName}'--");

        string[] options = [
            "1. Edit order info",
            "2. Edit coupons",
            "3. Edit pizzas",
            "4. Edit payment",
            "s. Save & Return",
            "q. Return without saving"
        ];
        TerminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = TerminalUI.PromptKey("Choose an option: ");
        TerminalUI.Clear();

        switch (choice) {
            case '1': await EditOrder(orderName, order.WithOrderInfo(await CreateOrderInfo())); break;
            case '2': await EditOrder(orderName, order.WithCoupons(await GetCoupons(order.OrderInfo.StoreId))); break;
            case '3': await EditOrder(orderName, order.WithPizzas(GetPizzas())); break;
            case '4': await EditOrder(orderName, order.WithPayment(GetPayment())); break;
            case 'S' or 's': SaveOrder(orderName, order); return;
            case 'Q' or 'q': return;
            default:
                TerminalUI.PrintLine("Not a valid option. Try again.");
                await EditOrder(orderName, order);
                break;
        }
    }

    private void SaveOrder(string orderName, SavedOrder order) {
        TerminalUI.PrintLine($"Saving '{orderName}' order:");

        Repo.SaveOrder(orderName, order);

        TerminalUI.Clear();
        TerminalUI.PrintLine("Order saved.");
    }

    private async Task CreateOrder() {
        var orderInfo = await CreateOrderInfo();
        if (orderInfo is null) return;

        var coupons = await GetCoupons(orderInfo.StoreId);

        var savedPizzas = GetPizzas();
        if (savedPizzas.Count == 0) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No pizzas selected.");
            return;
        }

        var payment = GetPayment();
        if (payment is not { } p) return;

        SavedOrder order = new() {
            Pizzas = savedPizzas,
            Coupons = coupons,
            OrderInfo = orderInfo,
            PaymentType = p.Type,
            PaymentInfoName = p.InfoName
        };

        var orderName = GetOrderName();
        TerminalUI.PrintLine($"Creating '{orderName}' order:");

        Repo.SaveOrder(orderName, order);

        TerminalUI.Clear();
        TerminalUI.PrintLine("Order saved.");
    }

    private (PaymentType Type, string? InfoName)? GetPayment() {
        var paymentType = GetPaymentType();
        if (paymentType is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No payment type selected.");
            return null;
        }

        string? paymentInfoName = null;
        if (paymentType == PaymentType.PayWithCard) {
            paymentInfoName = Chooser.GetUserChoice(
                "Choose a payment info to use: ", Repo.ListPayments(), "payment");
            if (paymentInfoName is null) {
                TerminalUI.Clear();
                TerminalUI.PrintLine("No payment info selected.");
                return null;
            }
        }

        TerminalUI.Clear();
        return (paymentType.Value, paymentInfoName);
    }

    private async Task<OrderInfo?> CreateOrderInfo() {
        var serviceMethod = GetServiceMethod();
        if (serviceMethod is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No service method selected.");
            return null;
        }

        var storeId = await GetStoreId(serviceMethod);
        if (storeId is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No store selected.");
            return null;
        }

        //TODO: Now/Later
        var timing = OrderTiming.Now.Instance;

        TerminalUI.Clear();
        return new UnvalidatedOrderInfo {
            StoreId = storeId,
            ServiceMethod = serviceMethod,
            Timing = timing
        }.Validate();
    }

    private string GetOrderName(string existingName = "") {
        string? orderName = TerminalUI.PromptForEdit("Order name: ", existingName);
        if (orderName is null) {
            TerminalUI.PrintLine("No order name entered. Try again.");
            return GetOrderName(existingName);
        }

        if (!orderName.IsValidName()) {
            TerminalUI.PrintLine("Invalid order name. Try again.");
            return GetOrderName(existingName);
        }

        if (Repo.ListOrders().Where(n => n != existingName).Contains(orderName)) {
            TerminalUI.PrintLine($"Order '{orderName}' already exists. Try again.");
            return GetOrderName(existingName);
        }

        TerminalUI.Clear();
        return orderName;
    }

    private List<SavedPizza> GetPizzas() {
        var pizzaNames = Chooser.GetUserChoices(
            "Choose pizzas to add to order: ", Repo.ListPizzas(), "pizza");
        if (pizzaNames.Count == 0) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No pizzas selected.");
            return [];
        }

        var quantities = GetQuantities(pizzaNames).ToList();
        if (quantities.Any(q => q is < 1 or > 25)) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("Invalid quantity.");
            return [];
        }
        return pizzaNames.Zip(quantities, (p, q) => new SavedPizza(p, q)).ToList();

        IEnumerable<int> GetQuantities(List<string> pizzaNames) {
            foreach (var pizzaName in pizzaNames) {
                var pizza = Repo.GetPizza(pizzaName)
                    ?? throw new InvalidOperationException("Pizza not found.");

                TerminalUI.PrintLine(pizza.Summarize());
                var input = TerminalUI.Prompt($"Quantity for {pizzaName} pizza: ") ?? "0";
                var quantity = int.TryParse(input, out int result) ? result : 0;
                yield return quantity;
            }
        }
    }

    private async Task<List<Coupon>> GetCoupons(string storeId) =>
        Chooser.GetUserChoices(
            "Choose coupons to add to order: ", await StoreApi.ListCoupons(new(storeId)), "coupon")
            .Select(c => new Coupon(c))
            .ToList();

    private async Task<string?> GetStoreId(ServiceMethod serviceMethod) {
        var zipCode = TerminalUI.Prompt("Zip code: ");
        StoreRequest request = new() {
            ServiceMethod = serviceMethod,
            ZipCode = zipCode
        };
        return Chooser.GetUserChoice(
            "Choose a store: ", await StoreApi.ListStores(request), "store");
    }

    private ServiceMethod GetServiceMethod() {
        string[] options = [
            "1. Delivery",
            "2. Carryout"
        ];

        TerminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = TerminalUI.PromptKey("Choose a service method: ");
        switch (choice) {
            case '1': return new ServiceMethod.Delivery(GetAddress());
            case '2': return new ServiceMethod.Carryout(GetPickupLocation());
            default:
                TerminalUI.Clear();
                TerminalUI.PrintLine("Not a valid option. Try again.");
                return GetServiceMethod();
        }

        PickupLocation GetPickupLocation() {
            string[] options = [
                "1. In store",
                "2. Drive thru",
                "3. Carside"
            ];

            TerminalUI.PrintLine(string.Join(Environment.NewLine, options));
            var choice = TerminalUI.PromptKey("Choose a pickup location: ");
            switch (choice) {
                case '1': return PickupLocation.InStore;
                case '2': return PickupLocation.DriveThru;
                case '3': return PickupLocation.Carside;
                default:
                    TerminalUI.Clear();
                    TerminalUI.PrintLine("Not a valid option. Try again.");
                    return GetPickupLocation();
            }
        }

        Address GetAddress() => throw new NotImplementedException();
    }

    private PaymentType? GetPaymentType() {
        string[] options = [
            "1. Pay at store",
            "2. Pay with card"
        ];

        TerminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = TerminalUI.PromptKey("Choose a payment type: ");
        switch (choice) {
            case '1': return PaymentType.PayAtStore;
            case '2': return PaymentType.PayWithCard;
            default:
                TerminalUI.Clear();
                TerminalUI.PrintLine("Not a valid option. Try again.");
                return null;
        }
    }

    private void DeleteOrder() {
        var orderName = Chooser.GetUserChoice(
            "Choose an order to delete: ", Repo.ListOrders(), "order");
        if (orderName is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No order selected.");
            return;
        }

        var order = Repo.GetOrder(orderName) ?? throw new Exception("Order not found.");
        TerminalUI.PrintLine($"Deleting '{orderName}' order:");
        TerminalUI.PrintLine(order.Summarize());
        var shouldDelete = IsAffirmative(TerminalUI.Prompt($"Delete order ({orderName}) [Y/n]: "));
        TerminalUI.Clear();

        if (shouldDelete) {
            Repo.DeleteOrder(orderName);
            TerminalUI.PrintLine("Order deleted.");
            return;
        }
        TerminalUI.PrintLine("Order not deleted.");
    }
}
