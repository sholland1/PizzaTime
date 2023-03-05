public class FastPizzaTests {
    [Theory]
    [InlineData("")]
    [InlineData("y")]
    public void HappyPathTest(string userChoice) {
        var consoleUI = new DummyConsoleUI(userChoice);
        var pizzaRepo = new DummyPizzaRepository();
        var pizzaApi = new DummyPizzaApi();
        var controller = new PizzaController(pizzaRepo, pizzaApi, consoleUI);

        controller.FastPizza();

        Assert.Equal($"""
            Pizza was added to cart:
            {pizzaApi.Calls[0].Result.Summarize()}
 
            Cart summary:
            {pizzaApi.Calls[1].Result.Summarize()}
 
            Confirm order? [Y/n]: 
            Ordering pizza...
            Order summary:
            {pizzaApi.Calls[2].Result.Summarize()}
            Done.
 
            """, consoleUI.ToString());
        Assert.Equal(3, pizzaApi.Calls.Count);
    }

    [Fact]
    public void CancelOrderTest() {
        var consoleUI = new DummyConsoleUI("n");
        var pizzaRepo = new DummyPizzaRepository();
        var pizzaApi = new DummyPizzaApi();
        var controller = new PizzaController(pizzaRepo, pizzaApi, consoleUI);

        controller.FastPizza();

        Assert.Equal($"""
            Pizza was added to cart:
            {pizzaApi.Calls[0].Result.Summarize()}
 
            Cart summary:
            {pizzaApi.Calls[1].Result.Summarize()}
 
            Confirm order? [Y/n]: 
            Order cancelled.
 
            """, consoleUI.ToString());
        Assert.Equal(2, pizzaApi.Calls.Count);
    }
}