using Controllers;
using Hollandsoft.OrderPizza;
using TestData;

namespace Tests;
public class FastPizzaTests {
    [Theory]
    [InlineData("")]
    [InlineData("y")]
    [InlineData("Y")]
    public async Task HappyPathTest(string userChoice) {
        DummyTerminalUI terminalUI = new(userChoice);
        DummyPizzaRepository repo = new();
        DummyPizzaCart cart = new();
        PizzaController controller = new(repo, _ => cart, terminalUI, default!, default!);

        await controller.FastPizza();

        object results(int i) => cart.Calls[i].Result;

        var pizzaResults = cart.Calls.Take(2).Select(x => x.Result).OfType<CartResult<AddPizzaSuccess>>().Select(x => x.SuccessValue).ToList();
        var summaryResult = ((CartResult<SummarySuccess>)results(2)).SuccessValue;
        var placeOrderResult = ((CartResult<string>)results(3)).SuccessValue;

        var expected = $"""
            Order ID: {pizzaResults[0]?.OrderID}

            Pizza was added to cart. Product Count: {pizzaResults[0]?.ProductCount}
            {repo.Pizzas.Values.ElementAt(0).Summarize()}

            Pizza was added to cart. Product Count: {pizzaResults[1]?.ProductCount}
            {repo.Pizzas.Values.ElementAt(1).Summarize()}

            Coupon {cart.Coupons.First().Code} was added to cart.

            Cart summary:
            {repo.GetDefaultOrder()?.OrderInfo?.Summarize()}
            Estimated Wait: {summaryResult?.WaitTime}
            Price: ${summaryResult?.TotalPrice}

            {repo.GetDefaultOrder()?.Payment?.Summarize()}

            Confirm order? [Y/n]: 
            Ordering pizza...
            Order summary:
            {placeOrderResult}
            Done.

            """;
        var actual = terminalUI.ToString();
        Assert.Equal(expected, actual);
        Assert.Equal(4, cart.Calls.Count);
    }

    [Fact]
    public async Task CancelOrderTest() {
        DummyTerminalUI terminalUI = new("n");
        DummyPizzaRepository repo = new();
        DummyPizzaCart cart = new();
        PizzaController controller = new(repo, _ => cart, terminalUI, default!, default!);

        await controller.FastPizza();

        var pizzaResults = cart.Calls.Take(2).Select(x => x.Result).OfType<CartResult<AddPizzaSuccess>>().Select(x => x.SuccessValue).ToList();
        var summaryResult = ((CartResult<SummarySuccess>)cart.Calls[2].Result).SuccessValue;

        var expected = $"""
            Order ID: {pizzaResults[0]?.OrderID}

            Pizza was added to cart. Product Count: {pizzaResults[0]?.ProductCount}
            {repo.Pizzas.Values.ElementAt(0).Summarize()}

            Pizza was added to cart. Product Count: {pizzaResults[1]?.ProductCount}
            {repo.Pizzas.Values.ElementAt(1).Summarize()}

            Coupon {cart.Coupons.First().Code} was added to cart.

            Cart summary:
            {repo.GetDefaultOrder()?.OrderInfo?.Summarize()}
            Estimated Wait: {summaryResult?.WaitTime}
            Price: ${summaryResult?.TotalPrice}

            {repo.GetDefaultOrder()?.Payment?.Summarize()}

            Confirm order? [Y/n]: 
            Order cancelled.

            """;
        var actual = terminalUI.ToString();
        Assert.Equal(expected, actual);
        Assert.Equal(3, cart.Calls.Count);
    }
}
