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

    public NamedOrder? GetDefaultOrder() => new("default", new UnvalidatedActualOrder {
            Pizzas = Pizzas.Values.Take(2).Select(p => p.Validate()).ToList(),
            Coupons = [new("1234")],
            OrderInfo = OrderInfos.First().Value.Validate(),
            Payment = Payments.First().Value.Validate()
        }.Validate());

    public void SavePersonalInfo(PersonalInfo personalInfo) => _personalInfo = personalInfo;
    public void SavePizza(string name, Pizza pizza) => Pizzas[name] = pizza;
    public void SavePayment(string name, Payment payment) => Payments[name] = payment;

    public IEnumerable<string> ListPizzas() => Pizzas.Keys;
    public IEnumerable<string> ListPayments() => Payments.Keys;

    public void DeletePizza(string name) => Pizzas.Remove(name);
    public void DeletePayment(string name) => Payments.Remove(name);

    public ActualOrder? GetOrder(string name) {
        throw new NotImplementedException();
    }

    public void SaveOrder(string name, SavedOrder order) {
        throw new NotImplementedException();
    }

    public IEnumerable<string> ListOrders() {
        throw new NotImplementedException();
    }

    public void DeleteOrder(string name) {
        throw new NotImplementedException();
    }

    private string? _defaultOrderName = "";
    public void SetDefaultOrder(string name) => _defaultOrderName = name;

    public SavedOrder? GetSavedOrder(string name) {
        throw new NotImplementedException();
    }

    public ActualOrder GetActualFromSavedOrder(SavedOrder order) {
        throw new NotImplementedException();
    }

    public void RenamePizza(string name, string newName) {
        throw new NotImplementedException();
    }

    public void RenamePayment(string name, string newName) {
        throw new NotImplementedException();
    }

    public void RenameOrder(string name, string newName) {
        throw new NotImplementedException();
    }

    public void AddOrderToHistory(PastOrder pastOrder) { }

    public IEnumerable<OrderInstance> ListPastOrders() {
        throw new NotImplementedException();
    }

    public PastOrder GetPastOrder(OrderInstance orderInstance) {
        throw new NotImplementedException();
    }
}

public class DummyTerminalUI(params string[] _readLines) : ITerminalUI {
    public List<string> PrintedMessages = [];
    private readonly Queue<string> _readLines = new(_readLines);
    public void Print(string message) => PrintedMessages.Add(message);
    public void PrintLine(string message) => PrintedMessages.Add(message + "\n");
    public void PrintLine() => PrintedMessages.Add("\n");
    public char? ReadKey() => _readLines.Dequeue().FirstOrDefault();
    public string? ReadLine() => _readLines.Dequeue();

    public override string ToString() => string.Join("", PrintedMessages);

    public string? EditLine(string lineToEdit) => ReadLine();
    public void Clear() { }

    public void SetCursorPosition(int left, int top) { }

    public void WriteInfoPanel(int hPos, IEnumerable<string> lines) { }
}

public class DummyPizzaCart(bool _cartFail = false, bool _priceFail = false, bool _orderFail = false) : ICart {
    public readonly HashSet<Coupon> Coupons = [];

    public List<MethodCall> Calls = [];

    private int _pizzaCount;
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
