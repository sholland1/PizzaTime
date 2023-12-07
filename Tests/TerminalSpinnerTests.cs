using Hollandsoft.PizzaTime;

namespace Tests;
public class TerminalSpinnerTests {
    [Fact]
    public async Task ShowAction() {
        // No stack overflows
        TerminalSpinner spinner = new("|-", 0);
        await spinner.Show("", () => { });
        await spinner.Show("", () => 0);
        await spinner.Show("", async () => await Task.CompletedTask);
        await spinner.Show("", async () => await Task.FromResult(0));
    }
}

