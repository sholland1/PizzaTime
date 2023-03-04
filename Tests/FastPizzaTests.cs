public class FastPizzaTests {
    [Theory]
    [InlineData("")]
    [InlineData("y")]
    public void HappyPathTest(string userChoice) {
        var consoleUI = new DummyConsoleUI(userChoice);
        var pizzaRepo = new DummyPizzaRepository();
        var pizzaApi = new DummyPizzaApi();
        var controller = new PizzaController(pizzaRepo, pizzaApi, consoleUI);
        var expectedPizza = pizzaRepo.GetPizza("defaultPizza");
        var expectedOrder = pizzaRepo.GetOrderInfo("defaultOrderInfo");
        var expectedPayment = pizzaRepo.GetPaymentInfo("defaultPaymentInfo");

        controller.FastPizza();

        Assert.Equal($"""
            Pizza was added to cart:
            {expectedPizza.Summarize()}
 
            Cart summary:
            {pizzaApi.AddPizzaToCart(expectedPizza).Summarize()}
 
            Confirm order? [Y/n]: 
            Ordering pizza...
            Order summary:
            {pizzaApi.OrderPizza(expectedOrder, expectedPayment).Summarize()}
            Done.
 
            """, consoleUI.ToString());
    }

    [Fact]
    public void CancelOrderTest() {
        var consoleUI = new DummyConsoleUI("n");
        var pizzaRepo = new DummyPizzaRepository();
        var pizzaApi = new DummyPizzaApi();
        var controller = new PizzaController(pizzaRepo, pizzaApi, consoleUI);
        var expectedPizza = pizzaRepo.GetPizza("defaultPizza");

        controller.FastPizza();

        Assert.Equal($"""
            Pizza was added to cart:
            {expectedPizza.Summarize()}
 
            Cart summary:
            {pizzaApi.AddPizzaToCart(expectedPizza).Summarize()}
 
            Confirm order? [Y/n]: 
            Order cancelled.
 
            """, consoleUI.ToString());
    }
}