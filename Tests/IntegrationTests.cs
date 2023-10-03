using Microsoft.Extensions.Configuration;

public class IntegrationTests {
    public IConfiguration Configuration { get; }
    public IntegrationTests() {
        var builder = new ConfigurationBuilder().AddUserSecrets<IntegrationTests>();
        Configuration = builder.Build();
    }

    //TODO: test a round trip from my format -> domino's format -> api -> domino's format
    [Fact(Skip = "integration test")]
    public async Task RealRoundTripOrder() {
        var storeID = int.Parse(Configuration["StoreID"]!);
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
    }

    private class TestDominosCart : DominosCart {
        public TestDominosCart(DominosConfig config, IOrderApi api) : base(config, api) { }
        public List<Product> Products => _products;
    }
}
