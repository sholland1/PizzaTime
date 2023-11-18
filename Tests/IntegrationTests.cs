using System.Diagnostics;
using Hollandsoft.OrderPizza;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TestData;

namespace Tests.Integration;
public class IntegrationTests {
    private IConfiguration Configuration { get; }
    public IntegrationTests() {
        var builder = new ConfigurationBuilder().AddUserSecrets<IntegrationTests>();
        Configuration = builder.Build();
    }

    [Fact(Skip = "integration test")]
    public async Task RealRoundTripOrder() {
        var storeID = Configuration["StoreID"]!;
        DominosOrderApi api = new(new DummyLogger<DominosOrderApi>(), MyJsonSerializer.Instance);

         UnvalidatedOrderInfo orderInfo = new() {
            StoreId = storeID,
            ServiceMethod = new ServiceMethod.Carryout(PickupLocation.DriveThru),
            Timing = OrderTiming.Now.Instance
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
        Assert.True(result.IsSuccess);
        Assert.Equal(products, cart.Products);

        UnvalidatedPersonalInfo personalInfo = new() {
            FirstName = "Test",
            LastName = "Testington",
            Email = "test@gmail.org",
            Phone = "000-123-1234"
        };
        UnvalidatedPayment payment = new(new PaymentInfo.PayWithCard("1000200030004000", "01/25", "123", "12345"));
        var finalResult = await cart.PlaceOrder(personalInfo.Validate(), payment.Validate());
        DebugWriteResult(finalResult);
        Assert.False(finalResult.IsSuccess);
    }

    private static void DebugWriteResult<T>(CartResult<T> result) where T : class {
        var message = result.Match(
            message => message,
            value => value?.ToString());
        Debug.WriteLine($"Success: {result.IsSuccess}, Message:\n{message}");
    }

    private class TestDominosCart : DominosCart {
        public TestDominosCart(IOrderApi api, OrderInfo orderInfo) : base(api, orderInfo) { }
        public List<Product> Products => _products;
    }

    private class DummyLogger<T> : ILogger<T> {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Debug.WriteLine(formatter(state, exception));
    }
}
