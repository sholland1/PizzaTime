using System.Text.Json;

namespace Tests;

public class SerializationTests {
    [Theory]
    [MemberData(nameof(TestPizza.GenerateValidPizzas), MemberType = typeof(TestPizza))]
    public void RoundTripA(TestPizza.ValidData p) {
        var serialized = JsonSerializer.Serialize(p.Pizza, PizzaSerializer.Options);
        var roundTrip = JsonSerializer.Deserialize<Pizza>(serialized, PizzaSerializer.Options);
        Assert.Equal(p.Pizza, roundTrip);
    }

    [Theory]
    [MemberData(nameof(TestPizza.GenerateValidPizzas), MemberType = typeof(TestPizza))]
    public void RoundTripB(TestPizza.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, p.JsonFile));
        var deserialized = JsonSerializer.Deserialize<Pizza>(json, PizzaSerializer.Options);
        var roundTrip = JsonSerializer.Serialize(deserialized, PizzaSerializer.Options);
        Assert.Equal(json, roundTrip);
    }

    [Theory]
    [MemberData(nameof(TestPizza.GenerateValidPizzas), MemberType = typeof(TestPizza))]
    public void DeserializeWorks(TestPizza.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, p.JsonFile));
        var deserialized = JsonSerializer.Deserialize<Pizza>(json, PizzaSerializer.Options);
        Assert.Equal(p.Pizza, deserialized);
    }

    [Theory]
    [MemberData(nameof(TestPizza.GenerateValidPizzas), MemberType = typeof(TestPizza))]
    public void SerializeWorks(TestPizza.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, p.JsonFile));
        var serialized = JsonSerializer.Serialize(p.Pizza, PizzaSerializer.Options);
        Assert.Equal(json, serialized);
    }

    [Theory]
    [MemberData(nameof(TestPizza.GenerateValidPizzas), MemberType = typeof(TestPizza))]
    public void DeserializeDoesntWorkForValidPizza(TestPizza.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, p.JsonFile));
        Assert.Throws<NotSupportedException>(() => {
            var deserialized = JsonSerializer.Deserialize<ValidPizza>(json, PizzaSerializer.Options);
            Assert.Fail($"Should throw before here. {deserialized}");
        });
    }

    [Theory]
    [MemberData(nameof(TestPizza.GenerateValidPizzas), MemberType = typeof(TestPizza))]
    public void SerializeWorksForValidPizza(TestPizza.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, p.JsonFile));
        p.Pizza.Parse().Match(
            vp => {
                var serialized = JsonSerializer.Serialize(vp, PizzaSerializer.Options);
                Assert.Equal(json, serialized);
            },
            es => Assert.Fail("Shouldn't fail."));
    }

    [Theory]
    [MemberData(nameof(TestPayment.GenerateValidPayments), MemberType = typeof(TestPayment))]
    public void PaymentInfoRoundTripA(TestPayment.ValidData p) {
        var serialized = JsonSerializer.Serialize(p.PaymentInfo, OrderSerializer.Options);
        var roundTrip = JsonSerializer.Deserialize<PaymentInfo>(serialized, OrderSerializer.Options);
        Assert.Equal(p.PaymentInfo, roundTrip);
    }

    [Theory]
    [MemberData(nameof(TestPayment.GenerateValidPayments), MemberType = typeof(TestPayment))]
    public void PaymentInfoRoundTripB(TestPayment.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestPayment.DataDirectory, p.JsonFile));
        var deserialized = JsonSerializer.Deserialize<PaymentInfo>(json, OrderSerializer.Options);
        var roundTrip = JsonSerializer.Serialize(deserialized, OrderSerializer.Options);
        Assert.Equal(json, roundTrip);
    }

    [Theory]
    [MemberData(nameof(TestPayment.GenerateValidPayments), MemberType = typeof(TestPayment))]
    public void PaymentInfoDeserializeWorks(TestPayment.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestPayment.DataDirectory, p.JsonFile));
        var deserialized = JsonSerializer.Deserialize<PaymentInfo>(json, OrderSerializer.Options);
        Assert.Equal(p.PaymentInfo, deserialized);
    }

    [Theory]
    [MemberData(nameof(TestPayment.GenerateValidPayments), MemberType = typeof(TestPayment))]
    public void PaymentInfoSerializationWorks(TestPayment.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestPayment.DataDirectory, p.JsonFile));
        var serialized = JsonSerializer.Serialize(p.PaymentInfo, OrderSerializer.Options);
        Assert.Equal(json, serialized);
    }


    [Theory]
    [MemberData(nameof(TestOrder.GenerateValidOrders), MemberType = typeof(TestOrder))]
    public void OrderInfoRoundTripA(TestOrder.ValidData p) {
        var serialized = JsonSerializer.Serialize(p.OrderInfo, OrderSerializer.Options);
        var roundTrip = JsonSerializer.Deserialize<OrderInfo>(serialized, OrderSerializer.Options);
        Assert.Equal(p.OrderInfo, roundTrip);
    }

    [Theory]
    [MemberData(nameof(TestOrder.GenerateValidOrders), MemberType = typeof(TestOrder))]
    public void OrderInfoRoundTripB(TestOrder.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestOrder.DataDirectory, p.JsonFile));
        var deserialized = JsonSerializer.Deserialize<OrderInfo>(json, OrderSerializer.Options);
        var roundTrip = JsonSerializer.Serialize(deserialized, OrderSerializer.Options);
        Assert.Equal(json, roundTrip);
    }

    [Theory]
    [MemberData(nameof(TestOrder.GenerateValidOrders), MemberType = typeof(TestOrder))]
    public void OrderInfoDeserializeWorks(TestOrder.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestOrder.DataDirectory, p.JsonFile));
        var deserialized = JsonSerializer.Deserialize<OrderInfo>(json, OrderSerializer.Options);
        Assert.Equal(p.OrderInfo, deserialized);
    }

    [Theory]
    [MemberData(nameof(TestOrder.GenerateValidOrders), MemberType = typeof(TestOrder))]
    public void OrderInfoSerializationWorks(TestOrder.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestOrder.DataDirectory, p.JsonFile));
        var serialized = JsonSerializer.Serialize(p.OrderInfo, OrderSerializer.Options);
        Assert.Equal(json, serialized);
    }
}