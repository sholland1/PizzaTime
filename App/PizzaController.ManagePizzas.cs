using Hollandsoft.OrderPizza;

namespace Controllers;
public partial class PizzaController {
    public async Task<Pizza?> CreatePizza() {
        var input = _editor.Edit();
        if (input is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No pizza created.");
            return default;
        }

        _terminalUI.SetCursorPosition(0, 0);

        var result = await _spinner.Show("Synthesizing pizza...", async () => await _aiPizzaBuilder.CreatePizza(input));
        _terminalUI.Clear();

        return await result.Match(async es => {
            _terminalUI.PrintLine("Failed to create pizza:");
            _terminalUI.PrintLine(string.Join(Environment.NewLine, es));
            var choice = IsAffirmative(_terminalUI.Prompt("Try again? [Y/n]: "));
            _terminalUI.Clear();
            return choice ? await CreatePizza() : default;
        }, p => {
            _terminalUI.PrintLine("New pizza:");
            _terminalUI.PrintLine(p.Summarize());
            var pizzaName = _terminalUI.Prompt("Pizza name: ") ?? "";
            var shouldSave = IsAffirmative(_terminalUI.Prompt($"Save pizza ({pizzaName})? [Y/n]: "));
            _terminalUI.Clear();

            if (shouldSave) {
                _repo.SavePizza(pizzaName, p);
                _terminalUI.PrintLine("Pizza saved.");
                return p;
            }
            _terminalUI.PrintLine("Pizza not saved.");
            return default;
        });
    }

    public async Task CreatePizzasMenu() {
        _terminalUI.PrintLine("--Manage Pizzas--");

        string[] options = new[] {
            "1. Create new pizza",
            "q. Return"
        };
        _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = _terminalUI.PromptKey("Choose an option: ");
        _terminalUI.Clear();

        switch (choice) {
            case '1': _ = await CreatePizza(); await ManagePizzasMenu(); break;
            case 'Q' or 'q': return;
            default:
                _terminalUI.PrintLine("Not a valid option. Try again.");
                await CreatePizzasMenu();
                break;
        }
    }

    public async Task ManagePizzasMenu() {
        if (!_repo.ListPizzas().Any()) {
            await CreatePizzasMenu();
            return;
        }

        _terminalUI.PrintLine("--Manage Pizzas--");

        string[] options = new[] {
            "1. Create new pizza",
            "2. Edit existing pizza",
            "3. Delete existing pizza",
            "q. Return"
        };
        _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = _terminalUI.PromptKey("Choose an option: ");
        _terminalUI.Clear();

        switch (choice) {
            case '1': _ = await CreatePizza(); await ManagePizzasMenu(); break;
            case '2': _ = await EditPizza(); await ManagePizzasMenu(); break;
            case '3': DeletePizza(); await ManagePizzasMenu(); break;
            case 'Q' or 'q': return;
            default:
                _terminalUI.PrintLine("Not a valid option. Try again.");
                await ManagePizzasMenu();
                break;
        }
    }

    private async Task<Pizza?> EditPizza() {
        var pizzaName = _chooser.GetUserChoice(
            "Choose a pizza to edit: ", _repo.ListPizzas(), "pizza");
        if (pizzaName is null) {
            _terminalUI.Clear();
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
            var choice = IsAffirmative(_terminalUI.Prompt("Try again? [Y/n]: "));
            _terminalUI.Clear();
            return choice ? await EditPizza() : default;
        }, p => {
            _terminalUI.PrintLine("Updated pizza:");
            _terminalUI.PrintLine(p.Summarize());
            var shouldSave = IsAffirmative(_terminalUI.Prompt($"Save pizza ({pizzaName})? [Y/n]: "));
            _terminalUI.Clear();

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
            _terminalUI.Clear();
            _terminalUI.PrintLine("No pizza selected.");
            return;
        }

        var pizza = _repo.GetPizza(pizzaName) ?? throw new Exception("Pizza not found.");
        _terminalUI.PrintLine($"Deleting '{pizzaName}' pizza:");
        _terminalUI.PrintLine(pizza.Summarize());
        var shouldDelete = IsAffirmative(_terminalUI.Prompt($"Delete pizza ({pizzaName})? [Y/n]: "));
        _terminalUI.Clear();

        if (shouldDelete) {
            _repo.DeletePizza(pizzaName);
            _terminalUI.PrintLine("Pizza deleted.");
            return;
        }
        _terminalUI.PrintLine("Pizza not deleted.");
    }
}
