using System.Text.Json;
using Hollandsoft.OrderPizza;
using static Hollandsoft.OrderPizza.BuilderHelpers;

namespace TestData;
public static class TestOrder {
    public const string DataDirectory = "../../../Data";
    public const string SummaryDirectory = "../../../Data/Summaries";

    public static IEnumerable<InvalidData2> BadEnumOrders() {
        yield return new(new("1", new ServiceMethod.Delivery(
            new((AddressType)10, "My House", "1234 Main St", null, "12345", "ACity", "NY")),
            new OrderTiming.Now()), new[] { "ServiceMethod.Address.AddressType" });
        yield return new(new("1", new ServiceMethod.Carryout((PickupLocation)10), new OrderTiming.Now()),
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
            new("1", new ServiceMethod.Carryout(PickupLocation.InStore), new OrderTiming.Now()),
            "defaultOrderInfo.json", "CarryoutNowSummary.txt");
        yield return new(
            new("2", new ServiceMethod.Delivery(new(AddressType.House, "My House", "1234 Main St", null, "12345", "ACity", "NY")), new OrderTiming.Now()),
            "DeliveryNow.json", "DeliveryNowSummary.txt");
        yield return new(
            new("3", new ServiceMethod.Delivery(new(AddressType.Business, "The Business", "1234 Main St", 123, "12345", "ACity", "NY")), new OrderTiming.Later(new(2021, 10, 30, 21, 30, 0))),
            "DeliveryLater.json", "DeliveryLaterSummary.txt");
        yield return new(
            new("4", new ServiceMethod.Carryout(PickupLocation.DriveThru), new OrderTiming.Later(new(2021, 10, 31, 21, 30, 0))),
            "CarryoutLater.json", "CarryoutLaterSummary.txt");
    }
}

public static class TestPayment {
    public const string DataDirectory = "../../../Data";
    public const string SummaryDirectory = "../../../Data/Summaries";

    public record ValidData(UnvalidatedPayment Payment, string JsonFile, string SummaryFile);
    public record InvalidData(string JsonFile, string[] InvalidProperties);

    public static IEnumerable<object[]> GenerateValidPayments() => ValidPayments().Select(p => new[] { p });
    public static IEnumerable<object[]> GenerateInvalidPayments() => InvalidPayments().Select(p => new[] { p });

    private static IEnumerable<InvalidData> InvalidPayments() {
        yield return new("InvalidPaymentInfo0.json", new[] { "PaymentInfo.BillingZip", "PaymentInfo.CardNumber", "PaymentInfo.Expiration", "PaymentInfo.SecurityCode"});
    }

    public static IEnumerable<ValidData> ValidPayments() {
        UnvalidatedPayment payAtStore = Payment.PayAtStoreInstance;
        UnvalidatedPayment payWithCard = new(new PaymentInfo.PayWithCard("1000200030004000", "01/23", "123", "12345"));

        yield return new(payWithCard, "defaultPaymentInfo.json", "PayWithCardSummary.txt");
        yield return new(payAtStore, "PayAtStoreInfo.json", "PayAtStoreSummary.txt");
    }
}

public static class TestPizza {
    public const string DataDirectory = "../../../Data";
    public const string SummaryDirectory = "../../../Data/Summaries";

    public record InvalidData(UnvalidatedPizza Pizza, string[] InvalidProperties);

    public static IEnumerable<object[]> GenerateValidPizzas() => ValidPizzas().Select(p => new[] { p });
    public static IEnumerable<object[]> GenerateInvalidPizzas() => InvalidPizzas().Select(p => new[] { p });

    public static IEnumerable<UnvalidatedPizza> ValidPizzas() {
        yield return LargePep.Build();
        yield return Complex.Build(2);
        yield return SmallThin.Build();
        yield return SmallHandTossed.Build();
        yield return XLPizza.Build(25);
    }

    public static IEnumerable<InvalidData> InvalidPizzas() =>
        JsonSerializer.Deserialize<IEnumerable<InvalidData>>(
            File.ReadAllText(Path.Combine(DataDirectory, "InvalidPizzas.json")),
            PizzaSerializer.Options)!;

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

    public static UnvalidatedPizza BadEnumPizza =>
        new((Size)10, (Crust)10, new Cheese.Full((Amount)10), new((SauceType)10, (Amount)10),
            new(new Topping[] { new((ToppingType)99, (Location)10, (Amount)10) }),
            (Bake)10, (Cut)10, false, false, 1);

    public static IPizzaBuilder Complex =>
        Build.Medium.Pan()
            .SetCheese(Light, Extra)
            .SetSauce(HoneyBBQ, Extra)
            .AddTopping(Pepperoni)
            .AddTopping(Bacon, Left, Extra)
            .AddTopping(Mushrooms, Right, Light)
            .AddTopping(Spinach)
            .SetBake(WellDone)
            .SetCut(Square);

    public static IPizzaBuilder LargePep =>
        Build.Large.HandTossed()
            .AddTopping(Pepperoni);

    public static IPizzaBuilder SmallThin =>
        Build.Small.Thin()
            .SetSauce(Marinara)
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
            .AddTopping(ShreddedProvoloneCheese, Right)
            .SetNoCheese()
            .WithOregano();

    public static IPizzaBuilder SmallHandTossed =>
        Build.Small.HandTossed()
            .SetBake(WellDone)
            .SetCheese()
            .WithGarlicCrust();

    public static IPizzaBuilder XLPizza =>
        Build.XL.Brooklyn()
            .AddTopping(Mushrooms, amount: Light)
            .AddTopping(Pineapple, amount: Light)
            .AddTopping(CheddarCheese, amount: Light)
            .AddTopping(GreenPeppers, amount: Light)
            .AddTopping(Spinach, amount: Light)
            // .AddTopping(RoastedRedPeppers, amount: Light)
            .AddTopping(FetaCheese, amount: Light)
            .AddTopping(ShreddedParmesanAsiago, amount: Light)
            .SetCheese(Light)
            .SetCut(Uncut);
}
