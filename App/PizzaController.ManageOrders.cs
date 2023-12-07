using Hollandsoft.PizzaTime;

namespace Controllers;
public partial class PizzaController {
    public async Task CreateOrdersMenu() {
        _terminalUI.PrintLine("--Manage Orders--");

        string[] options = [
            "1. Create new order",
            "q. Return"
        ];

        _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = _terminalUI.PromptKey("Choose an option: ");
        _terminalUI.Clear();

        switch (choice) {
            case '1': _ = CreateOrder(); await ManageOrdersMenu(); break;
            case 'Q' or 'q': return;
            default:
                _terminalUI.PrintLine("Not a valid option. Try again.");
                await CreateOrdersMenu();
                break;
        }
    }

    public async Task ManageOrdersMenu() {
        if (!_repo.ListOrders().Any()) {
            await CreateOrdersMenu();
            return;
        }

        _terminalUI.PrintLine("--Manage Orders--");

        string[] options = [
            "1. Set default order",
            "2. Create new order",
            "3. Edit existing order",
            "4. Delete existing order",
            "5. Rename existing order",
            "q. Return"
        ];
        _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = _terminalUI.PromptKey("Choose an option: ");
        _terminalUI.Clear();

        switch (choice) {
            case '1': SetDefaultOrder(); await ManageOrdersMenu(); break;
            case '2': await CreateOrder(); await ManageOrdersMenu(); break;
            case '3': await EditOrder(); await ManageOrdersMenu(); break;
            case '4': DeleteOrder(); await ManageOrdersMenu(); break;
            case '5': RenameOrder(); await ManageOrdersMenu(); break;
            case 'Q' or 'q': return;
            default:
                _terminalUI.PrintLine("Not a valid option. Try again.");
                await ManageOrdersMenu();
                break;
        }
    }

    private void RenameOrder() {
        var orderName = _chooser.GetUserChoice(
            "Choose an order to rename: ", _repo.ListOrders(), "order");
        if (orderName is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No order selected.");
            return;
        }

        var newOrderName = GetOrderName(orderName);
        if (orderName == newOrderName) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("Order not renamed.");
            return;
        }
        _repo.RenameOrder(orderName, newOrderName);

        _terminalUI.Clear();
        _terminalUI.PrintLine($"Order renamed to '{newOrderName}'.");
    }

    private void SetDefaultOrder() {
        var orderName = _chooser.GetUserChoice(
            "Choose an order to set as default: ", _repo.ListOrders(), "order");
        _terminalUI.Clear();
        if (orderName is null) {
            _terminalUI.PrintLine("No order selected.");
            return;
        }

        _repo.SetDefaultOrder(orderName);
        _terminalUI.PrintLine($"Default order set to '{orderName}'.");
    }

    private async Task EditOrder() {
        var orderName = _chooser.GetUserChoice(
            "Choose an order to edit: ", _repo.ListOrders(), "order");
        if (orderName is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No order selected.");
            return;
        }

        var order = _repo.GetSavedOrder(orderName) ?? throw new Exception("Order not found.");
        await EditOrder(orderName, order);
    }

    private async Task EditOrder(string orderName, SavedOrder order) {
        var actualOrder = _repo.GetActualFromSavedOrder(order);
        _terminalUI.WriteInfoPanel(50, actualOrder.Summarize().Split('\n'));
        _terminalUI.SetCursorPosition(0, 0);

        _terminalUI.PrintLine($"--Editing order '{orderName}'--");

        string[] options = [
            "1. Edit order info",
            "2. Edit coupons",
            "3. Edit pizzas",
            "4. Edit payment",
            "s. Save & Return",
            "q. Return without saving"
        ];
        _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = _terminalUI.PromptKey("Choose an option: ");
        _terminalUI.Clear();

        switch (choice) {
            case '1': await EditOrder(orderName, order.WithOrderInfo(await CreateOrderInfo())); break;
            case '2': await EditOrder(orderName, order.WithCoupons(await GetCoupons(order.OrderInfo.StoreId))); break;
            case '3': await EditOrder(orderName, order.WithPizzas(GetPizzas())); break;
            case '4': await EditOrder(orderName, order.WithPayment(GetPayment())); break;
            case 'S' or 's': SaveOrder(orderName, order); return;
            case 'Q' or 'q': return;
            default:
                _terminalUI.PrintLine("Not a valid option. Try again.");
                await EditOrder(orderName, order);
                break;
        }
    }

    private void SaveOrder(string orderName, SavedOrder order) {
        _terminalUI.PrintLine($"Saving '{orderName}' order:");

        _repo.SaveOrder(orderName, order);

        _terminalUI.Clear();
        _terminalUI.PrintLine("Order saved.");
    }

    private async Task CreateOrder() {
        var orderInfo = await CreateOrderInfo();
        if (orderInfo is null) return;

        var coupons = await GetCoupons(orderInfo.StoreId);

        var savedPizzas = GetPizzas();
        if (savedPizzas.Count == 0) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No pizzas selected.");
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
        _terminalUI.PrintLine($"Creating '{orderName}' order:");

        _repo.SaveOrder(orderName, order);

        _terminalUI.Clear();
        _terminalUI.PrintLine("Order saved.");
    }

    private (PaymentType Type, string? InfoName)? GetPayment() {
        var paymentType = GetPaymentType();
        if (paymentType is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No payment type selected.");
            return null;
        }

        string? paymentInfoName = null;
        if (paymentType == PaymentType.PayWithCard) {
            paymentInfoName = _chooser.GetUserChoice(
                "Choose a payment info to use: ", _repo.ListPayments(), "payment");
            if (paymentInfoName is null) {
                _terminalUI.Clear();
                _terminalUI.PrintLine("No payment info selected.");
                return null;
            }
        }

        _terminalUI.Clear();
        return (paymentType.Value, paymentInfoName);
    }

    private async Task<OrderInfo?> CreateOrderInfo() {
        var serviceMethod = GetServiceMethod();
        if (serviceMethod is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No service method selected.");
            return null;
        }

        var storeId = await GetStoreId(serviceMethod);
        if (storeId is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No store selected.");
            return null;
        }

        //TODO: Now/Later
        var timing = OrderTiming.Now.Instance;

        _terminalUI.Clear();
        return new UnvalidatedOrderInfo {
            StoreId = storeId,
            ServiceMethod = serviceMethod,
            Timing = timing
        }.Validate();
    }

    private string GetOrderName(string existingName = "") {
        string? orderName = _terminalUI.PromptForEdit("Order name: ", existingName);
        if (orderName is null) {
            _terminalUI.PrintLine("No order name entered. Try again.");
            return GetOrderName(existingName);
        }

        if (!orderName.IsValidName()) {
            _terminalUI.PrintLine("Invalid order name. Try again.");
            return GetOrderName(existingName);
        }

        if (_repo.ListOrders().Where(n => n != existingName).Contains(orderName)) {
            _terminalUI.PrintLine($"Order '{orderName}' already exists. Try again.");
            return GetOrderName(existingName);
        }

        _terminalUI.Clear();
        return orderName;
    }

    private List<SavedPizza> GetPizzas() {
        var pizzaNames = _chooser.GetUserChoices(
            "Choose pizzas to add to order: ", _repo.ListPizzas(), "pizza");
        if (pizzaNames.Count == 0) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No pizzas selected.");
            return [];
        }

        var quantities = GetQuantities(pizzaNames).ToList();
        if (quantities.Any(q => q is < 1 or > 25)) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("Invalid quantity.");
            return [];
        }
        return pizzaNames.Zip(quantities, (p, q) => new SavedPizza(p, q)).ToList();

        IEnumerable<int> GetQuantities(List<string> pizzaNames) {
            foreach (var pizzaName in pizzaNames) {
                var pizza = _repo.GetPizza(pizzaName)
                    ?? throw new InvalidOperationException("Pizza not found.");

                _terminalUI.PrintLine(pizza.Summarize());
                var input = _terminalUI.Prompt($"Quantity for {pizzaName} pizza: ") ?? "0";
                var quantity = int.TryParse(input, out int result) ? result : 0;
                yield return quantity;
            }
        }
    }

    private async Task<List<Coupon>> GetCoupons(string storeId) =>
        _chooser.GetUserChoices(
            "Choose coupons to add to order: ", await _storeApi.ListCoupons(new(storeId)), "coupon")
            .Select(c => new Coupon(c))
            .ToList();

    private async Task<string?> GetStoreId(ServiceMethod serviceMethod) {
        var zipCode = _terminalUI.Prompt("Zip code: ");
        StoreRequest request = new() {
            ServiceMethod = serviceMethod,
            ZipCode = zipCode
        };
        return _chooser.GetUserChoice(
            "Choose a store: ", await _storeApi.ListStores(request), "store");
    }

    private ServiceMethod GetServiceMethod() {
        string[] options = [
            "1. Delivery",
            "2. Carryout"
        ];

        _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = _terminalUI.PromptKey("Choose a service method: ");
        switch (choice) {
            case '1': return new ServiceMethod.Delivery(GetAddress());
            case '2': return new ServiceMethod.Carryout(GetPickupLocation());
            default:
                _terminalUI.Clear();
                _terminalUI.PrintLine("Not a valid option. Try again.");
                return GetServiceMethod();
        }

        PickupLocation GetPickupLocation() {
            string[] options = [
                "1. In store",
                "2. Drive thru",
                "3. Carside"
            ];

            _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
            var choice = _terminalUI.PromptKey("Choose a pickup location: ");
            switch (choice) {
                case '1': return PickupLocation.InStore;
                case '2': return PickupLocation.DriveThru;
                case '3': return PickupLocation.Carside;
                default:
                    _terminalUI.Clear();
                    _terminalUI.PrintLine("Not a valid option. Try again.");
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

        _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = _terminalUI.PromptKey("Choose a payment type: ");
        switch (choice) {
            case '1': return PaymentType.PayAtStore;
            case '2': return PaymentType.PayWithCard;
            default:
                _terminalUI.Clear();
                _terminalUI.PrintLine("Not a valid option. Try again.");
                return null;
        }
    }

    private void DeleteOrder() {
        var orderName = _chooser.GetUserChoice(
            "Choose an order to delete: ", _repo.ListOrders(), "order");
        if (orderName is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No order selected.");
            return;
        }

        var order = _repo.GetOrder(orderName) ?? throw new Exception("Order not found.");
        _terminalUI.PrintLine($"Deleting '{orderName}' order:");
        _terminalUI.PrintLine(order.Summarize());
        var shouldDelete = IsAffirmative(_terminalUI.Prompt($"Delete order ({orderName}) [Y/n]: "));
        _terminalUI.Clear();

        if (shouldDelete) {
            _repo.DeleteOrder(orderName);
            _terminalUI.PrintLine("Order deleted.");
            return;
        }
        _terminalUI.PrintLine("Order not deleted.");
    }
}
