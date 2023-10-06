using Microsoft.Extensions.Configuration;

public class IntegrationTests {
    private IConfiguration Configuration { get; }
    public IntegrationTests() {
        var builder = new ConfigurationBuilder().AddUserSecrets<IntegrationTests>();
        Configuration = builder.Build();
    }

    [Fact(Skip = "integration test")]
    public async Task RealRoundTripOrder() {
        var storeID = Configuration["StoreID"]!;
        DominosApi api = new();
        TestDominosCart cart = new(new() { StoreID = storeID }, api);

        var pizzas = TestPizza.ValidPizzas().Select(p => p.Validate()).ToList();

        foreach (var p in pizzas) {
            _ = await cart.AddPizza(p);
        }

        var products = pizzas.Select((p, i) => p.ToProduct(i + 1)).ToList();

        var result = await cart.GetSummary();
        Assert.True(result.Success);
        Assert.Equal(products, cart.Products);

        UnvalidatedPaymentInfo paymentInfo = new() {
            FirstName = "Test",
            LastName = "Testington",
            Email = "test@gmail.org",
            Phone = "000-123-1234",
            Payment = new Payment.PayWithCard(1000_2000_3000_4000, "01/25", "123", "12345")
        };
        var finalResult = await cart.PlaceOrder(null, paymentInfo.Validate());
        Assert.False(finalResult.Success);
    }

    private class TestDominosCart : DominosCart {
        public TestDominosCart(DominosConfig config, IOrderApi api) : base(config, api) { }
        public List<Product> Products => _products;
    }
}
