using static BuilderHelpers;
using static TestPizza;

public static class TestOrder {
    public const string DataDirectory = "../../../Data";

    public static IEnumerable<InvalidData2> BadEnumOrders() {
        yield return new(new(1, new ServiceMethod.Delivery(
            new((AddressType)10, "My House", "1234 Main St", null, "12345", "ACity", "NY")),
            new OrderTiming.Now()), new[] { "ServiceMethod.Address.AddressType" });
        yield return new(new(1, new ServiceMethod.Carryout((PickupLocation)10), new OrderTiming.Now()),
            new[] { "ServiceMethod.Location" });
    }

    public record ValidData(UnvalidatedOrderInfo OrderInfo, string JsonFile, string SummaryFile);
    public record InvalidData(string JsonFile, string[] InvalidProperties);
    public record InvalidData2(UnvalidatedOrderInfo BadEnumOrder, string[] InvalidProperties);

    public static IEnumerable<object[]> GenerateValidOrders() => ValidOrders().Select(o => new[] { o });
    public static IEnumerable<object[]> GenerateInvalidOrders() => InvalidOrders().Select(o => new[] { o });

    private static IEnumerable<InvalidData> InvalidOrders() {
        yield return new("InvalidOrderInfo0.json", new[] { "ServiceMethod.Address.Apt", "ServiceMethod.Address.State", "ServiceMethod.Address.ZipCode", "StoreId" });
    }

    public static IEnumerable<object[]> GenerateBadEnumOrders() => BadEnumOrders().Select(o => new[] { o });

    public static IEnumerable<ValidData> ValidOrders() {
        yield return new(
            new(1, new ServiceMethod.Carryout(PickupLocation.InStore), new OrderTiming.Now()),
            "defaultOrderInfo.json", "CarryoutNowSummary.txt");
        yield return new(
            new(2, new ServiceMethod.Delivery(new(AddressType.House, "My House", "1234 Main St", null, "12345", "ACity", "NY")), new OrderTiming.Now()),
            "DeliveryNow.json", "DeliveryNowSummary.txt");
        yield return new(
            new(3, new ServiceMethod.Delivery(new(AddressType.Business, "The Business", "1234 Main St", 123, "12345", "ACity", "NY")), new OrderTiming.Later(new(2021, 10, 30, 21, 30, 0))),
            "DeliveryLater.json", "DeliveryLaterSummary.txt");
        yield return new(
            new(4, new ServiceMethod.Carryout(PickupLocation.Window), new OrderTiming.Later(new(2021, 10, 31, 21, 30, 0))),
            "CarryoutLater.json", "CarryoutLaterSummary.txt");
    }
}

public static class TestPayment {
    public const string DataDirectory = "../../../Data";

    public record ValidData(UnvalidatedPaymentInfo PaymentInfo, string JsonFile, string SummaryFile);
    public record InvalidData(string JsonFile, string[] InvalidProperties);

    public static IEnumerable<object[]> GenerateValidPayments() => ValidPayments().Select(p => new[] { p });
    public static IEnumerable<object[]> GenerateInvalidPayments() => InvalidPayments().Select(p => new[] { p });

    private static IEnumerable<InvalidData> InvalidPayments() {
        yield return new("InvalidPaymentInfo0.json", new[] { "Email", "Payment.BillingZip", "Payment.CardNumber", "Payment.Expiration", "Payment.SecurityCode" , "Phone"});
    }

    public static IEnumerable<ValidData> ValidPayments() {
        UnvalidatedPaymentInfo payAtStore = new(
            "FName", "LName", "user@yahoo.com", "123-123-1234",
            new Payment.PayAtStore());
        Payment.PayWithCard cardPayment = new(1000_2000_3000_4000, "01/23", "123", "12345");
        UnvalidatedPaymentInfo payWithCard = new(
            "FName", "LName", "user@yahoo.com", "123-123-1234", cardPayment);

        yield return new(payWithCard, "defaultPaymentInfo.json", "PayWithCardSummary.txt");
        yield return new(payAtStore, "PayAtStoreInfo.json", "PayAtStoreSummary.txt");
    }
}

public static class TestPizza {
    public const string DataDirectory = "../../../Data";

    public record InvalidData(string JsonFile, string[] InvalidProperties);

    public static IEnumerable<object[]> GenerateValidPizzas() => ValidPizzas().Select(p => new[] { p });
    public static IEnumerable<object[]> GenerateInvalidPizzas() => InvalidPizzas().Select(p => new[] { p });

    public static IEnumerable<UnvalidatedPizza> ValidPizzas() {
        yield return LargePep.Build();
        yield return Complex.Build(2);
        yield return SmallThin.Build();
        yield return SmallHandTossed.Build();
        yield return XLPizza.Build(25);
    }

    public static IEnumerable<InvalidData> InvalidPizzas() {
        yield return new("InvalidPizza0.json", new[] { "Crust", "GarlicCrust", "Oregano", "Quantity", "Toppings" });
        yield return new("InvalidPizza1.json", new[] { "Crust", "Oregano", "Quantity", "Toppings" });
        yield return new("InvalidPizza2.json", new[] { "Bake", "Crust", "GarlicCrust", "Quantity", "Toppings" });
    }

    // public static void WritePizzaFile(int pizza, bool json = true, bool summary = true) {
    //     var p = ValidPizzas().ElementAt(pizza);
    //     if (json) {
    //         var text = System.Text.Json.JsonSerializer.Serialize(p.Pizza, PizzaSerializer.Options);
    //         File.WriteAllText(Path.Combine(DataDirectory, p.JsonFile), text);
    //     }

    //     if (summary) {
    //         File.WriteAllText(Path.Combine(DataDirectory, p.SummaryFile), p.Pizza.Summarize());
    //     }
    // }

    public static UnvalidatedPizza BadEnumPizza =
        new((Size)10, (Crust)10, new Cheese.Full((Amount)10), new((SauceType)10, (Amount)10),
            new(new Topping[] { new((ToppingType)99, (Location)10, (Amount)10) }),
            (Bake)10, (Cut)10, false, false, 1);

    public static IPizzaBuilder Complex =
        Build.Medium.Pan()
            .SetCheese(Light, Extra)
            .SetSauce(HoneyBBQ, Extra)
            .AddTopping(Pepperoni)
            .AddTopping(Bacon, Left, Extra)
            .AddTopping(Mushrooms, Right, Light)
            .AddTopping(Spinach)
            .SetBake(WellDone)
            .SetCut(Square);

    public static IPizzaBuilder LargePep =
        Build.Large.HandTossed()
            .AddTopping(Pepperoni);

    public static IPizzaBuilder SmallThin =
        Build.Small.Thin()
            .AddTopping(Pepperoni)
            .AddTopping(Ham, Left, Light)
            .AddTopping(Beef, Left, Light)
            .AddTopping(Salami, Left, Light)
            .AddTopping(ItalianSausage, Left, Light)
            .AddTopping(PremiumChicken, Left, Light)
            .AddTopping(Bacon, Left, Light)
            .AddTopping(PhillySteak, Left, Light)
            .AddTopping(HotBuffaloSauce, Left, Light)
            .AddTopping(JalapenoPeppers, Left, Light)
            .AddTopping(Onions, Right, Extra)
            .AddTopping(BananaPeppers, Right, Extra)
            .AddTopping(DicedTomatoes, Right, Extra)
            .AddTopping(BlackOlives, Right, Extra)
            .AddTopping(Mushrooms, Right)
            .SetNoCheese()
            .WithOregano();

    public static IPizzaBuilder SmallHandTossed =
        Build.Small.HandTossed()
            .SetBake(WellDone)
            .SetCheese()
            .WithGarlicCrust();

    public static IPizzaBuilder XLPizza =
        Build.XL.Brooklyn()
            .SetCheese(Light)
            .SetCut(Uncut);
}

public class DummyPizzaRepository : IPizzaRepo {
    private readonly Dictionary<string, UnvalidatedPizza> _pizzas = TestPizza.ValidPizzas().Zip(
        new[] {
            "defaultPizza",
            nameof(Complex),
            nameof(SmallThin),
            nameof(SmallHandTossed),
            nameof(XLPizza)
        })
        .ToDictionary(p => p.Second, p => p.First);

    private readonly Dictionary<string, OrderInfo> _orderInfos = TestOrder.ValidOrders()
        .ToDictionary(
            p => Path.GetFileNameWithoutExtension(p.JsonFile),
            p => p.OrderInfo.Validate());

    private readonly Dictionary<string, PaymentInfo> _paymentInfos = TestPayment.ValidPayments()
        .ToDictionary(
            p => Path.GetFileNameWithoutExtension(p.JsonFile),
            p => p.PaymentInfo.Validate());

    public Pizza GetPizza(string name) => _pizzas[name].Validate();
    public OrderInfo GetOrderInfo(string name) => _orderInfos[name];
    public PaymentInfo GetPaymentInfo(string name) => _paymentInfos[name];
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

public class DummyPizzaApi : IPizzaApi {
    private readonly bool _cartFail;
    private readonly bool _priceFail;
    private readonly bool _orderFail;

    public List<ApiCall> Calls = new();

    public DummyPizzaApi(bool cartFail = false, bool priceFail = false, bool orderFail = false) =>
        (_cartFail, _priceFail, _orderFail) = (cartFail, priceFail, orderFail);

    public Task<ApiResult> AddPizzaToCart(Pizza userPizza) {
        ApiResult result = new(!_cartFail,
            _cartFail
            ? "Pizza was not added to cart."
            : "Pizza added to cart.");
        Calls.Add(new(nameof(AddPizzaToCart), userPizza, result));
        return Task.FromResult(result);
    }

    public Task<ApiResult> GetCartSummary() {
        ApiResult result = new(!_priceFail,
            _priceFail
            ? "Failed to check cart price."
            : $"Cart price is ${Calls.Count(c => c.Method == nameof(AddPizzaToCart))*8.25:F2}.");
        Calls.Add(new(nameof(GetCartSummary), "", result));
        return Task.FromResult(result);
    }

    public Task<ApiResult> OrderPizza(OrderInfo userOrder, PaymentInfo userPayment) {
        ApiResult result = new(!_orderFail,
            _orderFail
            ? "Failed to place order."
            : "Order was placed.");
        Calls.Add(new(nameof(OrderPizza), (userOrder, userPayment), result));
        return Task.FromResult(result);
    }
}

public record ApiCall(string Method, object Body, ApiResult Result);
