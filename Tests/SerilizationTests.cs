using System.Text.Json;
using Hollandsoft.OrderPizza;
using TestData;

namespace Tests;
public class SerializationTests {
    [Fact]
    public void RoundTripA() {
        var pizzas = TestPizza.ValidPizzas();
        var serialized = JsonSerializer.Serialize(pizzas, PizzaSerializer.Options);
        var roundTrip = JsonSerializer.Deserialize<List<UnvalidatedPizza>>(serialized, PizzaSerializer.Options)!
            .Select(p => p.Validate());
        Assert.Equal(pizzas, roundTrip);
    }

    [Fact]
    public void RoundTripB() {
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, "ValidPizzas.json"));
        var deserialized = JsonSerializer.Deserialize<List<UnvalidatedPizza>>(json, PizzaSerializer.Options)!
            .Select(p => p.Validate());
        var roundTrip = JsonSerializer.Serialize(deserialized, PizzaSerializer.Options);
        Assert.Equal(json, roundTrip);
    }

    [Fact]
    public void DeserializeWorks() {
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, "ValidPizzas.json"));
        var deserialized = JsonSerializer.Deserialize<List<UnvalidatedPizza>>(json, PizzaSerializer.Options)!
            .Select(p => p.Validate());
        Assert.Equal(TestPizza.ValidPizzas(), deserialized);
    }

    [Fact]
    public void SerializeWorks() {
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, "ValidPizzas.json"));
        var serialized = JsonSerializer.Serialize(TestPizza.ValidPizzas(), PizzaSerializer.Options);
        Assert.Equal(json, serialized);
    }

    [Fact]
    public void DeserializeDoesntWorkForValidPizza() {
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, "ValidPizzas.json"));
        Assert.Throws<NotSupportedException>(() => {
            var deserialized = JsonSerializer.Deserialize<List<Pizza>>(json, PizzaSerializer.Options);
            Assert.Fail($"Should throw before here. {deserialized}");
        });
    }

    [Fact]
    public void SerializeWorksForValidPizza() {
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, "ValidPizzas.json"));
        var ps = TestPizza.ValidPizzas().Select(p => p.Validate());
        var serialized = JsonSerializer.Serialize(ps, PizzaSerializer.Options);
        Assert.Equal(json, serialized);
    }

    [Theory]
    [MemberData(nameof(TestPayment.GenerateValidPayments), MemberType = typeof(TestPayment))]
    public void PaymentInfoRoundTripA(TestPayment.ValidData p) {
        var serialized = JsonSerializer.Serialize(p.Payment, PizzaSerializer.Options);
        var roundTrip = JsonSerializer.Deserialize<UnvalidatedPayment>(serialized, PizzaSerializer.Options);
        Assert.Equal(p.Payment, roundTrip);
    }

    [Theory]
    [MemberData(nameof(TestPayment.GenerateValidPayments), MemberType = typeof(TestPayment))]
    public void PaymentInfoRoundTripB(TestPayment.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestPayment.DataDirectory, p.JsonFile));
        var deserialized = JsonSerializer.Deserialize<UnvalidatedPayment>(json, PizzaSerializer.Options);
        var roundTrip = JsonSerializer.Serialize(deserialized, PizzaSerializer.Options);
        Assert.Equal(json, roundTrip);
    }

    [Theory]
    [MemberData(nameof(TestPayment.GenerateValidPayments), MemberType = typeof(TestPayment))]
    public void PaymentInfoDeserializeWorks(TestPayment.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestPayment.DataDirectory, p.JsonFile));
        var deserialized = JsonSerializer.Deserialize<UnvalidatedPayment>(json, PizzaSerializer.Options);
        Assert.Equal(p.Payment, deserialized);
    }

    [Theory]
    [MemberData(nameof(TestPayment.GenerateValidPayments), MemberType = typeof(TestPayment))]
    public void PaymentInfoSerializationWorks(TestPayment.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestPayment.DataDirectory, p.JsonFile));
        var serialized = JsonSerializer.Serialize(p.Payment, PizzaSerializer.Options);
        Assert.Equal(json, serialized);
    }


    [Theory]
    [MemberData(nameof(TestOrder.GenerateValidOrders), MemberType = typeof(TestOrder))]
    public void OrderInfoRoundTripA(TestOrder.ValidData p) {
        var serialized = JsonSerializer.Serialize(p.OrderInfo, PizzaSerializer.Options);
        var roundTrip = JsonSerializer.Deserialize<UnvalidatedOrderInfo>(serialized, PizzaSerializer.Options);
        Assert.Equal(p.OrderInfo, roundTrip);
    }

    [Theory]
    [MemberData(nameof(TestOrder.GenerateValidOrders), MemberType = typeof(TestOrder))]
    public void OrderInfoRoundTripB(TestOrder.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestOrder.DataDirectory, p.JsonFile));
        var deserialized = JsonSerializer.Deserialize<UnvalidatedOrderInfo>(json, PizzaSerializer.Options);
        var roundTrip = JsonSerializer.Serialize(deserialized, PizzaSerializer.Options);
        Assert.Equal(json, roundTrip);
    }

    [Theory]
    [MemberData(nameof(TestOrder.GenerateValidOrders), MemberType = typeof(TestOrder))]
    public void OrderInfoDeserializeWorks(TestOrder.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestOrder.DataDirectory, p.JsonFile));
        var deserialized = JsonSerializer.Deserialize<UnvalidatedOrderInfo>(json, PizzaSerializer.Options);
        Assert.Equal(p.OrderInfo, deserialized);
    }

    [Theory]
    [MemberData(nameof(TestOrder.GenerateValidOrders), MemberType = typeof(TestOrder))]
    public void OrderInfoSerializationWorks(TestOrder.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestOrder.DataDirectory, p.JsonFile));
        var serialized = JsonSerializer.Serialize(p.OrderInfo, PizzaSerializer.Options);
        Assert.Equal(json, serialized);
    }
}
