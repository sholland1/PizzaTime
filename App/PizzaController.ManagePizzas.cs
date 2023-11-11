using Hollandsoft.OrderPizza;

namespace Controllers;
public partial class PizzaController {
    public async Task<Pizza?> CreatePizza() {
        _terminalUI.PrintLine("Describe your new pizza:");
        var input = _terminalUI.Prompt("> ") ?? "";
        var result = await _aiPizzaBuilder.CreatePizza(input);

        return await result.Match(async es => {
            _terminalUI.PrintLine("Failed to edit pizza:");
            foreach (var e in es) {
                _terminalUI.PrintLine(e);
            }
            var choice = _terminalUI.Prompt("Try again? [Y/n]: ");
            if (IsAffirmative(choice)) {
                return await CreatePizza();
            };
            return default;
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

    public async Task<Pizza?> ManagePizzas() {
        var newPizzaOption = "--create new--";
        var pizzaName = _chooser.GetUserChoice(
            "Choose a pizza to edit: ", _repo.ListPizzas().Prepend(newPizzaOption), "pizza");
        if (pizzaName is null) {
            _terminalUI.PrintLine("No pizza selected.");
            return default;
        }

        if (pizzaName == newPizzaOption) {
            return await CreatePizza();
        }

        var pizza = _repo.GetPizza(pizzaName) ?? throw new Exception("Pizza not found.");

        _terminalUI.PrintLine($"Editing {pizzaName}:");
        _terminalUI.PrintLine(pizza.Summarize());
        var input = _terminalUI.Prompt("> ") ?? "";
        var result = await _aiPizzaBuilder.EditPizza(pizza, input);

        return await result.Match(async es => {
            _terminalUI.PrintLine("Failed to edit pizza:");
            foreach (var e in es) {
                _terminalUI.PrintLine(e);
            }
            var choice = _terminalUI.Prompt("Try again? [Y/n]: ");
            if (IsAffirmative(choice)) {
                return await ManagePizzas();
            };
            return default;
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
}
