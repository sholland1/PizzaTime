using FluentValidation.Results;
using Hollandsoft.PizzaTime;
using TestData;

namespace Tests;
public class ValidationTests {
    private readonly MyJsonSerializer _serializer = MyJsonSerializer.Instance;

    [Theory]
    [MemberData(nameof(TestPizza.GenerateValidPizzas), MemberType = typeof(TestPizza))]
    public void ValidationWorks(UnvalidatedPizza p) => p.Validate();

    [Theory]
    [MemberData(nameof(TestPizza.GenerateInvalidPizzas), MemberType = typeof(TestPizza))]
    public void InvalidationWorks(TestPizza.InvalidData p) =>
        p.Pizza.Parse().Match(
            vp => Assert.Fail("Shouldn't be valid."),
            es => AssertSameInvalidProps(p.InvalidProperties, es));

    [Fact]
    public void InvalidationWorks2() {
        string[] invalidProperties = [
            "Bake", "Cheese.Amount", "Crust", "Crust", "Cut", "Sauce.Value.Amount", "Sauce.Value.SauceType",
            "Size", "Toppings[0].Amount", "Toppings[0].Location", "Toppings[0].ToppingType"
        ];

        TestPizza.BadEnumPizza.Parse().Match(
            vp => Assert.Fail("Shouldn't be valid."),
            es => AssertSameInvalidProps(invalidProperties, es));
    }

    [Theory]
    [MemberData(nameof(TestOrder.GenerateValidOrders), MemberType = typeof(TestOrder))]
    public void OrderInfoValidationWorks(TestOrder.ValidData o) => o.OrderInfo.Validate();

    [Theory]
    [MemberData(nameof(TestOrder.GenerateInvalidOrders), MemberType = typeof(TestOrder))]
    public void OrderInfoInvalidationWorks(TestOrder.InvalidData o) {
        var json = File.ReadAllText(Path.Combine(TestOrder.DataDirectory, o.JsonFile));
        var invalidOrder = _serializer.Deserialize<UnvalidatedOrderInfo>(json)!;
        invalidOrder.Parse().Match(
            vo => Assert.Fail("Shouldn't be valid."),
            es => AssertSameInvalidProps(o.InvalidProperties, es));
    }

    [Theory]
    [MemberData(nameof(TestOrder.GenerateBadEnumOrders), MemberType = typeof(TestOrder))]
    public void OrderInvalidationWorks2(TestOrder.InvalidData2 o) {
        o.BadEnumOrder.Parse().Match(
            vo => Assert.Fail("Shouldn't be valid."),
            es => AssertSameInvalidProps(o.InvalidProperties, es));
    }

    [Theory]
    [MemberData(nameof(TestPayment.GenerateValidPayments), MemberType = typeof(TestPayment))]
    public void PaymentInfoValidationWorks(TestPayment.ValidData p) =>
        p.Payment.Validate();

    [Theory]
    [MemberData(nameof(TestPayment.GenerateInvalidPayments), MemberType = typeof(TestPayment))]
    public void PaymentInfoInvalidationWorks(TestPayment.InvalidData p) {
        var json = File.ReadAllText(Path.Combine(TestPayment.DataDirectory, p.JsonFile));
        var invalidPayment = _serializer.Deserialize<UnvalidatedPayment>(json)!;
        invalidPayment.Parse().Match(
            vp => Assert.Fail("Shouldn't be valid."),
            es => AssertSameInvalidProps(p.InvalidProperties, es));
    }

    private static void AssertSameInvalidProps(string[] invalidProperties, List<ValidationFailure> errors) {
        var properties = errors
            .Select(e => e.PropertyName)
            .Order()
            .ToList();

        Assert.Multiple(
            () => Assert.Equal(invalidProperties.Length, errors.Count),
            () => Assert.Equal(invalidProperties, properties));
    }
}
