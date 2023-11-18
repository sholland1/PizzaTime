using Hollandsoft.OrderPizza;

namespace Controllers;
public partial class PizzaController {
    public async Task CreateOrdersMenu() {
        _terminalUI.PrintLine("--Manage Orders--");

        string[] options = new[] {
            "1. Create new order",
            "q. Return"
        };

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

        string[] options = new[] {
            "1. Set default order",
            "2. Create new order",
            "3. Edit existing order",
            "4. Delete existing order",
            "q. Return"
        };
        _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = _terminalUI.PromptKey("Choose an option: ");
        _terminalUI.Clear();

        switch (choice) {
            case '1': SetDefaultOrder(); await ManageOrdersMenu(); break;
            case '2': await CreateOrder(); await ManageOrdersMenu(); break;
            case '3': await EditOrder(); await ManageOrdersMenu(); break;
            case '4': DeleteOrder(); await ManageOrdersMenu(); break;
            case 'Q' or 'q': return;
            default:
                _terminalUI.PrintLine("Not a valid option. Try again.");
                await ManageOrdersMenu();
                break;
        }
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

    private Task EditOrder() {
        throw new NotImplementedException();
    }

    private async Task CreateOrder() {
        var savedPizzas = GetPizzas();
        if (!savedPizzas.Any()) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No pizzas selected.");
            return;
        }

        //TODO: Add coupons
        var coupons = await GetCoupons();

        //TODO: Choose store
        var storeId = await GetStoreId();
        if (storeId is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No store selected.");
            return;
        }

        var serviceMethod = GetServiceMethod();
        if (serviceMethod is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No service method selected.");
            return;
        }

        //TODO: Now/Later
        var timing = OrderTiming.Now.Instance;

        var paymentType = GetPaymentType();
        if (paymentType is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No payment type selected.");
            return;
        }

        string? paymentInfoName = null;
        if (paymentType == PaymentType.PayWithCard) {
            paymentInfoName = _chooser.GetUserChoice(
                "Choose a payment info to use: ", _repo.ListPayments(), "payment");
            if (paymentInfoName is null) {
                _terminalUI.Clear();
                _terminalUI.PrintLine("No payment info selected.");
                return;
            }
        }

        SavedOrder order = new() {
            Pizzas = savedPizzas,
            Coupons = coupons,
            OrderInfo = new UnvalidatedOrderInfo {
                StoreId = $"{storeId}",
                ServiceMethod = serviceMethod,
                Timing = timing
            }.Validate(),
            PaymentType = paymentType.Value,
            PaymentInfoName = paymentInfoName
        };

        var orderName = _terminalUI.Prompt("Order name: ") ?? "";
        _terminalUI.PrintLine($"Creating '{orderName}' order:");

        _repo.SaveOrder(orderName, order);

        _terminalUI.Clear();
        _terminalUI.PrintLine("Order saved.");
    }

    private List<SavedPizza> GetPizzas() {
        var pizzaNames = _chooser.GetUserChoices(
            "Choose pizzas to add to order: ", _repo.ListPizzas(), "pizza");
        if (!pizzaNames.Any()) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No pizzas selected.");
            return new();
        }

        //TODO: Add quantities
        var quantities = GetQuantities(pizzaNames).ToList();
        if (quantities.Any(q => q is < 1 or > 25)) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("Invalid quantity.");
            return new();
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

    private async Task<List<Coupon>> GetCoupons() {
        // return _chooser.GetUserChoices(
        //     "Choose coupons to add to order: ", await _api.ListCoupons(), "coupon");
        return new() { new("0000") };
    }

    private async Task<int?> GetStoreId() {
        // return _chooser.GetUserChoice(
        //     "Choose a store: ", await _api.ListNearbyStores(), "store");
        return 0;
    }

    private ServiceMethod GetServiceMethod() {
        var options = new[] {
            "1. Delivery",
            "2. Carryout"
        };

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
            var options = new[] {
                "1. In store",
                "2. Drive thru",
                "3. Carside"
            };

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
        var options = new[] {
            "1. Pay at store",
            "2. Pay with card"
        };

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
