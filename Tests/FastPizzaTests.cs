public class FastPizzaTests {
    [Theory]
    [InlineData("")]
    [InlineData("y")]
    [InlineData("Y")]
    public async Task HappyPathTest(string userChoice) {
        DummyConsoleUI consoleUI = new(userChoice);
        DummyPizzaRepository pizzaRepo = new();
        DummyPizzaCart pizzaApi = new();
        PizzaController controller = new(pizzaRepo, _ => pizzaApi, consoleUI);

        await controller.FastPizza();

        var expected = $"""
            Pizza was added to cart.
            {pizzaApi.Calls[0].Result.Summarize()}

            Cart summary:
            {pizzaApi.Calls[1].Result.Summarize()}

            Confirm order? [Y/n]: 
            Ordering pizza...
            Order summary:
            {pizzaApi.Calls[2].Result.Summarize()}
            Done.

            """;
        var actual = consoleUI.ToString();
        Assert.Equal(expected, actual);
        Assert.Equal(3, pizzaApi.Calls.Count);
    }

    [Fact]
    public async Task CancelOrderTest() {
        DummyConsoleUI consoleUI = new("n");
        DummyPizzaRepository pizzaRepo = new();
        DummyPizzaCart pizzaApi = new();
        PizzaController controller = new(pizzaRepo, _ => pizzaApi, consoleUI);

        await controller.FastPizza();

        var expected = $"""
            Pizza was added to cart.
            {pizzaApi.Calls[0].Result.Summarize()}

            Cart summary:
            {pizzaApi.Calls[1].Result.Summarize()}

            Confirm order? [Y/n]: 
            Order cancelled.

            """;
        var actual = consoleUI.ToString();
        Assert.Equal(expected, actual);
        Assert.Equal(2, pizzaApi.Calls.Count);
    }
}
