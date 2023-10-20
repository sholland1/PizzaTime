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

        CartResult results(int i) => cart.Calls[i].Result;

        var pizzaResults = cart.Calls.Take(2).Select(x => x.Result).OfType<AddPizzaResult>().ToList();
        var summaryResult = (SummaryResult)cart.Calls[2].Result;

        var expected = $"""
            Order ID: {pizzaResults[0].OrderID}

            {pizzaResults[0].Message} Product Count: {pizzaResults[0].ProductCount}
            {repo.Pizzas.Values.ElementAt(0).Summarize()}

            {pizzaResults[1].Message} Product Count: {pizzaResults[1].ProductCount}
            {repo.Pizzas.Values.ElementAt(1).Summarize()}

            Coupon {cart.Coupons.First().Code} was added to cart.

            Cart summary:
            {repo.GetDefaultOrder()?.OrderInfo?.Summarize()}
            Estimated Wait: {summaryResult.WaitTime}
            Price: ${summaryResult.TotalPrice}

            {repo.GetDefaultPaymentInfo()!.Summarize()}

            Confirm order? [Y/n]: 
            Ordering pizza...
            Order summary:
            {results(3).Message}
            Done.

            """;
        var actual = consoleUI.ToString();
        Assert.Equal(expected, actual);
        Assert.Equal(4, cart.Calls.Count);
    }

    [Fact]
    public async Task CancelOrderTest() {
        DummyConsoleUI consoleUI = new("n");
        DummyPizzaRepository repo = new();
        DummyPizzaCart cart = new();
        PizzaController controller = new(repo, _ => cart, consoleUI);

        await controller.FastPizza();

        var pizzaResults = cart.Calls.Take(2).Select(x => x.Result).OfType<AddPizzaResult>().ToList();
        var summaryResult = (SummaryResult)cart.Calls[2].Result;

        var expected = $"""
            Order ID: {pizzaResults[0].OrderID}

            {pizzaResults[0].Message} Product Count: {pizzaResults[0].ProductCount}
            {repo.Pizzas.Values.ElementAt(0).Summarize()}

            {pizzaResults[1].Message} Product Count: {pizzaResults[1].ProductCount}
            {repo.Pizzas.Values.ElementAt(1).Summarize()}

            Coupon {cart.Coupons.First().Code} was added to cart.

            Cart summary:
            {repo.GetDefaultOrder()?.OrderInfo?.Summarize()}
            Estimated Wait: {summaryResult.WaitTime}
            Price: ${summaryResult.TotalPrice}

            {repo.GetDefaultPaymentInfo()!.Summarize()}

            Confirm order? [Y/n]: 
            Order cancelled.

            """;
        var actual = consoleUI.ToString();
        Assert.Equal(expected, actual);
        Assert.Equal(3, cart.Calls.Count);
    }
}
