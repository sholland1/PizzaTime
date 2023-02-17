using System.Text.Json;
using FluentValidation.Results;

namespace Tests;

public class ValidationTests {
    [Theory]
    [MemberData(nameof(TestPizza.GenerateValidPizzas), MemberType = typeof(TestPizza))]
    public void ValidationWorks(TestPizza.ValidData p) =>
        p.Pizza.Parse().Match(
            vp => Assert.True(true),
            es => Assert.Fail("Shouldn't be invalid."));

    [Theory]
    [MemberData(nameof(TestPizza.GenerateInvalidPizzas), MemberType = typeof(TestPizza))]
    public void InvalidationWorks(TestPizza.InvalidData p) {
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, p.JsonFile));
        var invalidPizza = JsonSerializer.Deserialize<Pizza>(json, PizzaSerializer.Options)!;
        invalidPizza.Parse().Match(
            vp => Assert.Fail("Shouldn't be valid."),
            es => AssertSameInvalidProps(p.InvalidProperties, es));
    }

    [Fact]
    public void InvalidationWorks2() {
        var invalidProperties = new[] {
            "Bake", "Cheese.Amount", "Crust", "Cut", "Sauce.Value.Amount", "Sauce.Value.SauceType",
            "Size", "Toppings[0].Amount", "Toppings[0].Location", "Toppings[0].ToppingType"
        };

        TestPizza.BadEnumPizza.Parse().Match(
            vp => Assert.Fail("Shouldn't be valid."),
            es => AssertSameInvalidProps(invalidProperties, es));
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
