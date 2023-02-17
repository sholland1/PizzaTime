using static BuilderHelpers;

//create test data for OrderInfo
public static class TestOrder {
    public const string DataDirectory = "../../../Data";

    public record ValidData(OrderInfo OrderInfo, string JsonFile, string SummaryFile);
    // public record InvalidData(string JsonFile, string[] InvalidProperties);

    public static IEnumerable<object[]> GenerateValidOrders() => ValidOrders().Select(o => new[] { o });
    // public static IEnumerable<object[]> GenerateInvalidOrders() => InvalidOrders().Select(o => new[] { o });

    public static IEnumerable<ValidData> ValidOrders() {
        yield return new(
            new(1, new ServiceMethod.Delivery(new(AddressType.House, "My House", "1234 Main St", null, "12345", "ACity", "NY")), new OrderTiming.Now()),
            "DeliveryNow.json", "DeliveryNowSummary.txt");
        yield return new(
            new(2, new ServiceMethod.Delivery(new(AddressType.Business, "The Business", "1234 Main St", 123, "12345", "ACity", "NY")), new OrderTiming.Later(new(2021, 10, 30, 21, 30, 0))),
            "DeliveryLater.json", "DeliveryLaterSummary.txt");
        yield return new(
            new(3, new ServiceMethod.Carryout(PickupLocation.InStore), new OrderTiming.Now()),
            "CarryoutNow.json", "CarryoutNowSummary.txt");
        yield return new(
            new(4, new ServiceMethod.Carryout(PickupLocation.Window), new OrderTiming.Later(new(2021, 10, 31, 21, 30, 0))),
            "CarryoutLater.json", "CarryoutLaterSummary.txt");
    }
}

public static class TestPayment {
    public const string DataDirectory = "../../../Data";

    public record ValidData(PaymentInfo PaymentInfo, string JsonFile, string SummaryFile);

    public static IEnumerable<object[]> GenerateValidPayments() => ValidPayments().Select(p => new[] { p });

    public static IEnumerable<ValidData> ValidPayments() {
        PaymentInfo payAtStore = new(
            "FName", "LName", "user@yahoo.com", "123-123-1234",
            new Payment.PayAtStore());
        Payment.PayWithCard cardPayment = new(1000_2000_3000_4000, "01/23", "123", "12345");

        yield return new(payAtStore, "PayAtStoreInfo.json", "PayAtStoreSummary.txt");
        yield return new(payAtStore with { Payment = cardPayment }, "PayWithCardInfo.json", "PayWithCardSummary.txt");
    }
}

public static class TestPizza {
    public const string DataDirectory = "../../../Data";

    public record ValidData(Pizza Pizza, string JsonFile, string SummaryFile);
    public record InvalidData(string JsonFile, string[] InvalidProperties);

    public static IEnumerable<object[]> GenerateValidPizzas() => ValidPizzas().Select(p => new[] { p });
    public static IEnumerable<object[]> GenerateInvalidPizzas() => InvalidPizzas().Select(p => new[] { p });

    public static IEnumerable<ValidData> ValidPizzas() {
        yield return new(Complex.Build(2), "ComplexPizza.json", "ComplexPizzaSummary.txt");
        yield return new(LargePep.Build(), "LargePepPizza.json", "LargePepPizzaSummary.txt");
        yield return new(SmallThin.Build(), "SmallThinPizza.json", "SmallThinPizzaSummary.txt");
        yield return new(SmallHandTossed.Build(), "SmallHandTossed.json", "SmallHandTossedSummary.txt");
        yield return new(XLPizza.Build(25), "XLPizza.json", "XLPizzaSummary.txt");
    }

    public static IEnumerable<InvalidData> InvalidPizzas() {
        yield return new("InvalidPizza0.json", new[] { "Crust", "DippingSauce.GarlicAmount", "DippingSauce.RanchAmount", "GarlicCrust", "Oregano", "Quantity", "Toppings" });
        yield return new("InvalidPizza1.json", new[] { "Crust", "Oregano", "Quantity", "Toppings" });
        yield return new("InvalidPizza2.json", new[] { "Bake", "Crust", "GarlicCrust", "Quantity", "Toppings" });
    }

    public static void WritePizzaFile(int pizza, bool json = true, bool summary = true) {
        var p = ValidPizzas().ElementAt(pizza);
        if (json) {
            var text = System.Text.Json.JsonSerializer.Serialize(p.Pizza, PizzaSerializer.Options);
            File.WriteAllText(Path.Combine(DataDirectory, p.JsonFile), text);
        }

        if (summary) {
            File.WriteAllText(Path.Combine(DataDirectory, p.SummaryFile), p.Pizza.Summarize());
        }
    }

    public static Pizza BadEnumPizza =
        new((Size)10, (Crust)10, new Cheese.Full((Amount)10), new((SauceType)10, (Amount)10),
            new(new Topping[] { new((ToppingType)99, (Location)10, (Amount)10) }),
            new(0, 0, 0), (Bake)10, (Cut)10, false, false, 1);

    public static IPizzaBuilder Complex =
        Build.Medium.Pan()
            .SetCheese(None, Extra)
            .SetSauce(Marinara, Extra)
            .AddTopping(Pepperoni)
            .AddTopping(Bacon, Left, Extra)
            .AddTopping(Mushrooms, Right, Light)
            .AddTopping(Spinach)
            .SetDippingSauces(1, 2, 3)
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
            .SetDippingSauces(25, 25, 25)
            .SetCut(Uncut);
}