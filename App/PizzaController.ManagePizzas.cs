using Hollandsoft.OrderPizza;

namespace Controllers;
public partial class PizzaController {
    public async Task<Pizza?> CreatePizza() {
        var input = Editor.Create();
        if (input is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No pizza created.");
            return default;
        }

        TerminalUI.SetCursorPosition(0, 0);

        var result = await Spinner.Show("Synthesizing pizza...", async () => await AiPizzaBuilder.CreatePizza(input));
        TerminalUI.Clear();

        return await result.Match(async es => {
            TerminalUI.PrintLine("Failed to create pizza:");
            TerminalUI.PrintLine(string.Join(Environment.NewLine, es));
            var choice = IsAffirmative(TerminalUI.Prompt("Try again? [Y/n]: "));
            TerminalUI.Clear();
            return choice ? await CreatePizza() : default;
        }, p => {
            TerminalUI.PrintLine("New pizza:");
            TerminalUI.PrintLine(p.Summarize());
            var pizzaName = GetPizzaName();
            var shouldSave = IsAffirmative(TerminalUI.Prompt($"Save pizza ({pizzaName})? [Y/n]: "));
            TerminalUI.Clear();

            if (shouldSave) {
                Repo.SavePizza(pizzaName, p);
                TerminalUI.PrintLine("Pizza saved.");
                return p;
            }
            TerminalUI.PrintLine("Pizza not saved.");
            return default;
        });
    }

    private string GetPizzaName(string existingName = "") {
        string? pizzaName = TerminalUI.PromptForEdit("Pizza name: ", existingName);
        if (pizzaName is null) {
            TerminalUI.PrintLine("No pizza name entered. Try again.");
            return GetPizzaName(existingName);
        }

        if (!pizzaName.IsValidName()) {
            TerminalUI.PrintLine("Invalid pizza name. Try again.");
            return GetPizzaName(existingName);
        }

        if (Repo.ListPizzas().Where(n => n != existingName).Contains(pizzaName)) {
            TerminalUI.PrintLine($"Pizza '{pizzaName}' already exists. Try again.");
            return GetPizzaName(existingName);
        }

        TerminalUI.Clear();
        return pizzaName;
    }

    public async Task CreatePizzasMenu() {
        TerminalUI.PrintLine("--Manage Pizzas--");

        string[] options = [
            "1. Create new pizza",
            "q. Return"
        ];
        TerminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = TerminalUI.PromptKey("Choose an option: ");
        TerminalUI.Clear();

        switch (choice) {
            case '1': _ = await CreatePizza(); await ManagePizzasMenu(); break;
            case 'Q' or 'q': return;
            default:
                TerminalUI.PrintLine("Not a valid option. Try again.");
                await CreatePizzasMenu();
                break;
        }
    }

    public async Task ManagePizzasMenu() {
        if (!Repo.ListPizzas().Any()) {
            await CreatePizzasMenu();
            return;
        }

        TerminalUI.PrintLine("--Manage Pizzas--");

        string[] options = [
            "1. Create new pizza",
            "2. Edit existing pizza",
            "3. Delete existing pizza",
            "4. Rename existing pizza",
            "q. Return"
        ];
        TerminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = TerminalUI.PromptKey("Choose an option: ");
        TerminalUI.Clear();

        switch (choice) {
            case '1': _ = await CreatePizza(); await ManagePizzasMenu(); break;
            case '2': _ = await EditPizza(); await ManagePizzasMenu(); break;
            case '3': DeletePizza(); await ManagePizzasMenu(); break;
            case '4': RenamePizza(); await ManagePizzasMenu(); break;
            case 'Q' or 'q': return;
            default:
                TerminalUI.PrintLine("Not a valid option. Try again.");
                await ManagePizzasMenu();
                break;
        }
    }

    private void RenamePizza() {
        var pizzaName = Chooser.GetUserChoice(
            "Choose a pizza to rename: ", Repo.ListPizzas(), "pizza");
        if (pizzaName is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No pizza selected.");
            return;
        }

        var newPizzaName = GetPizzaName(pizzaName);
        if (pizzaName == newPizzaName) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("Pizza not renamed.");
            return;
        }
        Repo.RenamePizza(pizzaName, newPizzaName);

        TerminalUI.Clear();
        TerminalUI.PrintLine($"Pizza renamed to '{newPizzaName}'.");
    }

    private async Task<Pizza?> EditPizza() {
        var pizzaName = Chooser.GetUserChoice(
            "Choose a pizza to edit: ", Repo.ListPizzas(), "pizza");
        if (pizzaName is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No pizza selected.");
            return default;
        }

        var pizza = Repo.GetPizza(pizzaName) ?? throw new Exception("Pizza not found.");

        var input = Editor.Edit(pizzaName, pizza);
        if (input is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No pizza created.");
            return default;
        }

        TerminalUI.SetCursorPosition(0, 0);

        var result = await Spinner.Show("Synthesizing pizza...", async () => await AiPizzaBuilder.EditPizza(pizza, input));
        TerminalUI.Clear();

        return await result.Match(async es => {
            TerminalUI.PrintLine("Failed to edit pizza:");
            TerminalUI.PrintLine(string.Join(Environment.NewLine, es));
            var choice = IsAffirmative(TerminalUI.Prompt("Try again? [Y/n]: "));
            TerminalUI.Clear();
            return choice ? await EditPizza() : default;
        }, p => {
            TerminalUI.PrintLine("Updated pizza:");
            TerminalUI.PrintLine(p.Summarize());
            var shouldSave = IsAffirmative(TerminalUI.Prompt($"Save pizza ({pizzaName})? [Y/n]: "));
            TerminalUI.Clear();

            if (shouldSave) {
                Repo.SavePizza(pizzaName, p);
                TerminalUI.PrintLine("Pizza saved.");
                return p;
            }
            TerminalUI.PrintLine("Pizza not saved.");
            return default;
        });
    }

    private void DeletePizza() {
        var pizzaName = Chooser.GetUserChoice(
            "Choose a pizza to delete: ", Repo.ListPizzas(), "pizza");
        if (pizzaName is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No pizza selected.");
            return;
        }

        var pizza = Repo.GetPizza(pizzaName) ?? throw new Exception("Pizza not found.");
        TerminalUI.PrintLine($"Deleting '{pizzaName}' pizza:");
        TerminalUI.PrintLine(pizza.Summarize());
        var shouldDelete = IsAffirmative(TerminalUI.Prompt($"Delete pizza ({pizzaName})? [Y/n]: "));
        TerminalUI.Clear();

        if (shouldDelete) {
            Repo.DeletePizza(pizzaName);
            TerminalUI.PrintLine("Pizza deleted.");
            return;
        }
        TerminalUI.PrintLine("Pizza not deleted.");
    }
}
