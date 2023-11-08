using static Hollandsoft.OrderPizza.CartResult;
using static TestData.TestPizza;
using Hollandsoft.OrderPizza;

namespace TestData;
public class DummyPizzaRepository : IPizzaRepo {
    public Dictionary<string, UnvalidatedOrderInfo> OrderInfos { get; } = TestOrder.ValidOrders()
        .ToDictionary(
            p => Path.GetFileNameWithoutExtension(p.JsonFile),
            p => p.OrderInfo);

    public Dictionary<string, UnvalidatedPayment> Payments { get; } = TestPayment.ValidPayments()
        .ToDictionary(
            p => Path.GetFileNameWithoutExtension(p.JsonFile),
            p => p.Payment);

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
    public Payment GetPayment(string name) => Payments[name].Validate();

    private PersonalInfo? _personalInfo = new UnvalidatedPersonalInfo {
        FirstName = "Test",
        LastName = "Testington",
        Email = "test@gmail.org",
        Phone = "000-123-1234"
    }.Validate();

    public PersonalInfo? GetPersonalInfo() => _personalInfo;

    public NewOrder? GetDefaultOrder() => new UnvalidatedOrder {
        Pizzas = Pizzas.Values.Take(2).ToList(),
        Coupons = new() { new("1234") },
        OrderInfo = OrderInfos.First().Value,
        PaymentType = PaymentType.PayWithCard
    }.Validate();

    public Payment? GetDefaultPayment() => Payments.First().Value.Validate();

    public void SavePersonalInfo(PersonalInfo personalInfo) => _personalInfo = personalInfo;

    public void SavePizza(string name, Pizza pizza) => Pizzas[name] = pizza;

    public IEnumerable<string> ListPizzas() => Pizzas.Keys;

    public void SavePayment(string name, Payment payment) => Payments[name] = payment;
}

public class DummyConsoleUI : IConsoleUI {
    public List<string> PrintedMessages = new();
    private readonly Queue<string> _readLines = new();

    public DummyConsoleUI(params string[] readLines) => Array.ForEach(readLines, _readLines.Enqueue);

    public void Print(string message) => PrintedMessages.Add(message);
    public void PrintLine(string message) => PrintedMessages.Add(message + "\n");
    public void PrintLine() => PrintedMessages.Add("\n");
    public char? ReadKey() => _readLines.Dequeue().FirstOrDefault();
    public string? ReadLine() => _readLines.Dequeue();

    public override string ToString() => string.Join("", PrintedMessages);

    public string? EditLine(string _) => ReadLine();
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
    public Task<CartResult<AddPizzaSuccess>> AddPizza(Pizza userPizza) {
        var result = _cartFail
            ? AddPizzaFailure("Pizza was not added to cart.")
            : Success(new AddPizzaSuccess(++_pizzaCount, "test"));
        Calls.Add(new(nameof(AddPizza), userPizza, result));
        return Task.FromResult(result);
    }

    public Task<CartResult<SummarySuccess>> GetSummary() {
        var orderTotal = 8.25m * Calls.Count(c => c.Method == nameof(AddPizza));
        var result = _priceFail
            ? SummaryFailure("Failed to check cart price.")
            : Success(new SummarySuccess(orderTotal, "10-15 minutes"));
        Calls.Add(new(nameof(GetSummary), "", result));
        return Task.FromResult(result);
    }

    public Task<CartResult<string>> PlaceOrder(PersonalInfo personalInfo, Payment userPayment) {
        var result = _orderFail
            ? PlaceOrderFailure("Failed to check cart price.")
            : Success("Order was placed.");
        Calls.Add(new(nameof(PlaceOrder), (personalInfo, userPayment), result));
        return Task.FromResult(result);
    }

    public void AddCoupon(Coupon coupon) => Coupons.Add(coupon);
    public void RemoveCoupon(Coupon coupon) => Coupons.Remove(coupon);
}

public record MethodCall(string Method, object Body, object Result);
