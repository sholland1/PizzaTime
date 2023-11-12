using Hollandsoft.OrderPizza;

namespace Controllers;
public partial class PizzaController {
    public async Task CreateOrdersMenu() {
        string[] options = new[] {
            "1. Create new order",
            "q. Return"
        };
        _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = _terminalUI.PromptKey("Choose an option: ");

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

        string[] options = new[] {
            "1. Set default order",
            "2. Create new order",
            "3. Edit existing order",
            "4. Delete existing order",
            "q. Return"
        };
        _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = _terminalUI.PromptKey("Choose an option: ");

        switch (choice) {
            case '1': SetDefaultOrder(); await ManageOrdersMenu(); break;
            case '2': _ = CreateOrder(); await ManageOrdersMenu(); break;
            case '3': _ = EditOrder(); await ManageOrdersMenu(); break;
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
        if (orderName is null) {
            _terminalUI.PrintLine("No order selected.");
            return;
        }

        _repo.SetDefaultOrder(orderName);
        _terminalUI.PrintLine($"Default order set to '{orderName}'.");
    }

    private ActualOrder? EditOrder() {
        throw new NotImplementedException();
    }

    private ActualOrder? CreateOrder() {
        throw new NotImplementedException();
    }

    private void DeleteOrder() {
        var orderName = _chooser.GetUserChoice(
            "Choose an order to delete: ", _repo.ListOrders(), "order");
        if (orderName is null) {
            _terminalUI.PrintLine("No order selected.");
            return;
        }

        var order = _repo.GetOrder(orderName) ?? throw new Exception("Order not found.");
        _terminalUI.PrintLine($"Deleting '{orderName}' order:");
        _terminalUI.PrintLine(order.Summarize());
        var shouldDelete = IsAffirmative(_terminalUI.Prompt($"Delete order ({orderName}) [Y/n]: "));
        if (shouldDelete) {
            _repo.DeleteOrder(orderName);
            _terminalUI.PrintLine("Order deleted.");
            return;
        }
        _terminalUI.PrintLine("Order not deleted.");
    }
}
