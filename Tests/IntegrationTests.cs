using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class IntegrationTests {
    private IConfiguration Configuration { get; }
    public IntegrationTests() {
        var builder = new ConfigurationBuilder().AddUserSecrets<IntegrationTests>();
        Configuration = builder.Build();
    }

    [Fact(Skip = "integration test")]
    public async Task RealRoundTripOrder() {
        var storeID = Configuration["StoreID"]!;
        DominosApi api = new(new DummyLogger<DominosApi>());

         UnvalidatedOrderInfo orderInfo = new() {
            StoreId = storeID,
            ServiceMethod = new ServiceMethod.Carryout(PickupLocation.DriveThru),
            Timing = new OrderTiming.Now()
        };
        TestDominosCart cart = new(api, orderInfo.Validate());

        var pizzas = TestPizza.ValidPizzas().Select(p => p.Validate()).ToList();

        foreach (var p in pizzas) {
            var pResult = await cart.AddPizza(p);
            DebugWriteResult(pResult);
            Thread.Sleep(1000);
        }

        var products = pizzas.Select((p, i) => p.ToProduct(i + 1)).ToList();
        Coupon coupon = new("9220");
        cart.AddCoupon(coupon);

        var result = await cart.GetSummary();
        DebugWriteResult(result);
        Assert.True(result.Success);
        Assert.Equal(products, cart.Products);

        UnvalidatedPaymentInfo paymentInfo = new() {
            FirstName = "Test",
            LastName = "Testington",
            Email = "test@gmail.org",
            Phone = "000-123-1234",
            Payment = new Payment.PayWithCard("1000200030004000", "01/25", "123", "12345")
        };
        var finalResult = await cart.PlaceOrder(paymentInfo.Validate());
        DebugWriteResult(finalResult);
        Assert.False(finalResult.Success);
    }

    private static void DebugWriteResult(CartResult result) =>
        Debug.WriteLine($"Success: {result.Success}, Message:\n{result.Message}");

    private class TestDominosCart : DominosCart {
        public TestDominosCart(IOrderApi api, OrderInfo orderInfo) : base(api, orderInfo) { }
        public List<Product> Products => _products;
    }
}

class DummyLogger<T> : ILogger<T> {
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Debug.WriteLine(formatter(state, exception));
}
