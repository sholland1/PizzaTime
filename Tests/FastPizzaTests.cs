public class FastPizzaTests {
    [Theory]
    [InlineData("")]
    [InlineData("y")]
    [InlineData("Y")]
    public async Task HappyPathTest(string userChoice) {
        DummyConsoleUI consoleUI = new(userChoice);
        DummyPizzaRepository repo = new();
        DummyPizzaCart cart = new();
        PizzaController controller = new(repo, _ => cart, consoleUI);

        await controller.FastPizza();

        var expected = $"""
            Pizza was added to cart.
            {cart.Calls[0].Result.Summarize()}

            Coupon {cart.Coupons.First().Code} was added to cart.

            Cart summary:
            {cart.Calls[1].Result.Summarize()}

            Confirm order? [Y/n]: 
            Ordering pizza...
            Order summary:
            {cart.Calls[2].Result.Summarize()}
            Done.

            """;
        var actual = consoleUI.ToString();
        Assert.Equal(expected, actual);
        Assert.Equal(3, cart.Calls.Count);
    }

    [Fact]
    public async Task CancelOrderTest() {
        DummyConsoleUI consoleUI = new("n");
        DummyPizzaRepository repo = new();
        DummyPizzaCart cart = new();
        PizzaController controller = new(repo, _ => cart, consoleUI);

        await controller.FastPizza();

        var expected = $"""
            Pizza was added to cart.
            {cart.Calls[0].Result.Summarize()}

            Coupon {cart.Coupons.First().Code} was added to cart.

            Cart summary:
            {cart.Calls[1].Result.Summarize()}

            Confirm order? [Y/n]: 
            Order cancelled.

            """;
        var actual = consoleUI.ToString();
        Assert.Equal(expected, actual);
        Assert.Equal(2, cart.Calls.Count);
    }
}
