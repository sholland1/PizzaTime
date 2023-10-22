using static TestData.TestPizza;
using Hollandsoft.OrderPizza;

namespace TestData;
public class DummyPizzaRepository : IPizzaRepo {
    public Dictionary<string, UnvalidatedOrderInfo> OrderInfos { get; } = TestOrder.ValidOrders()
        .ToDictionary(
            p => Path.GetFileNameWithoutExtension(p.JsonFile),
            p => p.OrderInfo);

    public Dictionary<string, UnvalidatedPaymentInfo> PaymentInfos { get; } = TestPayment.ValidPayments()
        .ToDictionary(
            p => Path.GetFileNameWithoutExtension(p.JsonFile),
            p => p.PaymentInfo);

    public Dictionary<string, UnvalidatedPizza> Pizzas { get; } = ValidPizzas().Zip(
        new[] {
            "defaultPizza",
            nameof(Complex),
            nameof(SmallThin),
            nameof(SmallHandTossed),
            nameof(XLPizza)
        })
        .ToDictionary(p => p.Second, p => p.First);

    public Pizza GetPizza(string name) => Pizzas[name].Validate();
    public OrderInfo GetOrderInfo(string name) => OrderInfos[name].Validate();
    public PaymentInfo GetPaymentInfo(string name) => PaymentInfos[name].Validate();

    public PersonalInfo? GetPersonalInfo() => new UnvalidatedPersonalInfo {
        FirstName = "Test",
        LastName = "Testington",
        Email = "test@gmail.org",
        Phone = "000-123-1234"
    }.Validate();

    public NewOrder? GetDefaultOrder() => new UnvalidatedOrder {
        Pizzas = Pizzas.Values.Take(2).ToList(),
        Coupons = new() { new("1234") },
        OrderInfo = OrderInfos.First().Value,
        PaymentType = PaymentType.PayWithCard
    }.Validate();

    public PaymentInfo? GetDefaultPaymentInfo() => PaymentInfos.First().Value.Validate();
}

public class DummyConsoleUI : IConsoleUI {
    public List<string> PrintedMessages = new List<string>();
    private readonly Queue<string> _readLines = new Queue<string>();

    public DummyConsoleUI(params string[] readLines) => Array.ForEach(readLines, _readLines.Enqueue);

    public void Print(string message) => PrintedMessages.Add(message);
    public void PrintLine(string message) => PrintedMessages.Add(message + "\n");
    public void PrintLine() => PrintedMessages.Add("\n");
    public string? ReadLine() => _readLines.Dequeue();

    public override string ToString() => string.Join("", PrintedMessages);
}

public class DummyPizzaCart : ICart {
    private readonly bool _cartFail;
    private readonly bool _priceFail;
    private readonly bool _orderFail;
    public readonly HashSet<Coupon> Coupons = new();

    public List<MethodCall> Calls = new();

    public DummyPizzaCart(bool cartFail = false, bool priceFail = false, bool orderFail = false) =>
        (_cartFail, _priceFail, _orderFail) = (cartFail, priceFail, orderFail);

    private int _pizzaCount = 0;
    public Task<AddPizzaResult> AddPizza(Pizza userPizza) {
        AddPizzaResult result = new(!_cartFail,
            _cartFail
            ? "Pizza was not added to cart."
            : "Pizza was added to cart.") {
                ProductCount = ++_pizzaCount,
                OrderID = "test"
            };
        Calls.Add(new(nameof(AddPizza), userPizza, result));
        return Task.FromResult(result);
    }

    public Task<SummaryResult> GetSummary() {
        var orderTotal = 8.25m * Calls.Count(c => c.Method == nameof(AddPizza));
        SummaryResult result = new(!_priceFail,
            _priceFail
            ? "Failed to check cart price."
            : $"Cart price is ${orderTotal:F2}.") {
                WaitTime = "10-15 minutes",
                TotalPrice = orderTotal
            };
        Calls.Add(new(nameof(GetSummary), "", result));
        return Task.FromResult(result);
    }

    public Task<CartResult> PlaceOrder(PersonalInfo personalInfo, PaymentInfo userPayment) {
        CartResult result = new(!_orderFail,
            _orderFail
            ? "Failed to place order."
            : "Order was placed.");
        Calls.Add(new(nameof(PlaceOrder), (personalInfo, userPayment), result));
        return Task.FromResult(result);
    }

    public void AddCoupon(Coupon coupon) => Coupons.Add(coupon);
    public void RemoveCoupon(Coupon coupon) => Coupons.Remove(coupon);
}

public record MethodCall(string Method, object Body, CartResult Result);
