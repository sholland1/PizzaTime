using Hollandsoft.OrderPizza;

namespace Controllers;
public partial class PizzaController {
    public async Task<Pizza?> CreatePizza() {
        _terminalUI.PrintLine("Describe your new pizza:");
        var input = _terminalUI.Prompt("> ") ?? "";
        var result = await _aiPizzaBuilder.CreatePizza(input);

        return await result.Match(async es => {
            _terminalUI.PrintLine("Failed to create pizza:");
            _terminalUI.PrintLine(string.Join(Environment.NewLine, es));
            var choice = _terminalUI.Prompt("Try again? [Y/n]: ");
            return IsAffirmative(choice) ? await CreatePizza() : default;
        }, p => {
            _terminalUI.PrintLine("New pizza:");
            _terminalUI.PrintLine(p.Summarize());
            var pizzaName = _terminalUI.Prompt("Pizza name: ") ?? "";
            var shouldSave = IsAffirmative(_terminalUI.Prompt($"Save pizza ({pizzaName})? [Y/n]: "));
            if (shouldSave) {
                _repo.SavePizza(pizzaName, p);
                _terminalUI.PrintLine("Pizza saved.");
                return p;
            }
            _terminalUI.PrintLine("Pizza not saved.");
            return default;
        });
    }

    public async Task ManagePizzas() {
        string[] options = new[] {
            "1. Create new pizza",
            "2. Edit existing pizza",
            "3. Delete existing pizza",
            "q. Return"
        };
        _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = _terminalUI.PromptKey("Choose an option: ");

        switch (choice) {
            case '1': _ = await CreatePizza(); await ManagePizzas(); break;
            case '2': _ = await EditPizza(); await ManagePizzas(); break;
            case '3': DeletePizza(); await ManagePizzas(); break;
            case 'Q' or 'q': return;
            default:
                _terminalUI.PrintLine("Not a valid option. Try again.");
                await ManagePizzas();
                break;
        }
    }

    private async Task<Pizza?> EditPizza() {
        var pizzaName = _chooser.GetUserChoice(
            "Choose a pizza to edit: ", _repo.ListPizzas(), "pizza");
        if (pizzaName is null) {
            _terminalUI.PrintLine("No pizza selected.");
            return default;
        }

        var pizza = _repo.GetPizza(pizzaName) ?? throw new Exception("Pizza not found.");

        _terminalUI.PrintLine($"Editing '{pizzaName}' pizza:");
        _terminalUI.PrintLine(pizza.Summarize());
        var input = _terminalUI.Prompt("> ") ?? "";
        var result = await _aiPizzaBuilder.EditPizza(pizza, input);

        return await result.Match(async es => {
            _terminalUI.PrintLine("Failed to edit pizza:");
            _terminalUI.PrintLine(string.Join(Environment.NewLine, es));
            var choice = _terminalUI.Prompt("Try again? [Y/n]: ");
            return IsAffirmative(choice) ? await EditPizza() : default;
        }, p => {
            _terminalUI.PrintLine("Updated pizza:");
            _terminalUI.PrintLine(p.Summarize());
            var shouldSave = IsAffirmative(_terminalUI.Prompt($"Save pizza ({pizzaName})? [Y/n]: "));
            if (shouldSave) {
                _repo.SavePizza(pizzaName, p);
                _terminalUI.PrintLine("Pizza saved.");
                return p;
            }
            _terminalUI.PrintLine("Pizza not saved.");
            return default;
        });
    }

    private void DeletePizza() {
        var pizzaName = _chooser.GetUserChoice(
            "Choose a pizza to delete: ", _repo.ListPizzas(), "pizza");
        if (pizzaName is null) {
            _terminalUI.PrintLine("No pizza selected.");
            return;
        }

        var pizza = _repo.GetPizza(pizzaName) ?? throw new Exception("Pizza not found.");
        _terminalUI.PrintLine($"Deleting '{pizzaName}' pizza:");
        _terminalUI.PrintLine(pizza.Summarize());
        var shouldDelete = IsAffirmative(_terminalUI.Prompt($"Delete pizza ({pizzaName})? [Y/n]: "));
        if (shouldDelete) {
            _repo.DeletePizza(pizzaName);
            _terminalUI.PrintLine("Pizza deleted.");
            return;
        }
        _terminalUI.PrintLine("Pizza not deleted.");
    }
}
