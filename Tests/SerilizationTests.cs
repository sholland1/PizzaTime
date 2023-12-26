using Hollandsoft.PizzaTime;
using TestData;

namespace Tests;
public class SerializationTests {
    private readonly MyJsonSerializer _serializer = MyJsonSerializer.Instance;

    [Fact]
    public void RoundTripA() {
        var pizzas = TestPizza.ValidPizzas();
        var serialized = _serializer.Serialize(pizzas);
        var roundTrip = _serializer.Deserialize<List<UnvalidatedPizza>>(serialized)!
            .Select(p => p.Validate());
        Assert.Equal(pizzas, roundTrip);
    }

    [Fact]
    public void RoundTripB() {
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, "ValidPizzas.json"));
        var deserialized = _serializer.Deserialize<List<UnvalidatedPizza>>(json)!
            .Select(p => p.Validate());
        var roundTrip = _serializer.Serialize(deserialized);
        Assert.Equal(json, roundTrip);
    }

    [Fact]
    public void DeserializeWorks() {
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, "ValidPizzas.json"));
        var deserialized = _serializer.Deserialize<List<UnvalidatedPizza>>(json)!
            .Select(p => p.Validate());
        Assert.Equal(TestPizza.ValidPizzas(), deserialized);
    }

    [Fact]
    public void SerializeWorks() {
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, "ValidPizzas.json"));
        var serialized = _serializer.Serialize(TestPizza.ValidPizzas());
        Assert.Equal(json, serialized);
    }

    [Fact]
    public void DeserializeDoesntWorkForValidPizza() {
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, "ValidPizzas.json"));
        Assert.Throws<NotSupportedException>(() => {
            var deserialized = _serializer.Deserialize<List<Pizza>>(json);
            Assert.Fail($"Should throw before here. {deserialized}");
        });
    }

    [Fact]
    public void SerializeWorksForValidPizza() {
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, "ValidPizzas.json"));
        var ps = TestPizza.ValidPizzas().Select(p => p.Validate());
        var serialized = _serializer.Serialize(ps);
        Assert.Equal(json, serialized);
    }

    [Theory]
    [MemberData(nameof(TestPayment.GenerateValidPayments), MemberType = typeof(TestPayment))]
    public void PaymentInfoRoundTripA(TestPayment.ValidData p) {
        var serialized = _serializer.Serialize(p.Payment);
        var roundTrip = _serializer.Deserialize<UnvalidatedPayment>(serialized);
        Assert.Equal(p.Payment, roundTrip);
    }

    [Theory]
    [MemberData(nameof(TestPayment.GenerateValidPayments), MemberType = typeof(TestPayment))]
    public void PaymentInfoRoundTripB(TestPayment.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestPayment.DataDirectory, p.JsonFile));
        var deserialized = _serializer.Deserialize<UnvalidatedPayment>(json);
        var roundTrip = _serializer.Serialize(deserialized);
        Assert.Equal(json, roundTrip);
    }

    [Theory]
    [MemberData(nameof(TestPayment.GenerateValidPayments), MemberType = typeof(TestPayment))]
    public void PaymentInfoDeserializeWorks(TestPayment.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestPayment.DataDirectory, p.JsonFile));
        var deserialized = _serializer.Deserialize<UnvalidatedPayment>(json);
        Assert.Equal(p.Payment, deserialized);
    }

    [Theory]
    [MemberData(nameof(TestPayment.GenerateValidPayments), MemberType = typeof(TestPayment))]
    public void PaymentInfoSerializationWorks(TestPayment.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestPayment.DataDirectory, p.JsonFile));
        var serialized = _serializer.Serialize(p.Payment);
        Assert.Equal(json, serialized);
    }

    [Theory]
    [MemberData(nameof(TestOrder.GenerateValidOrders), MemberType = typeof(TestOrder))]
    public void OrderInfoRoundTripA(TestOrder.ValidData p) {
        var serialized = _serializer.Serialize(p.OrderInfo);
        var roundTrip = _serializer.Deserialize<UnvalidatedOrderInfo>(serialized);
        Assert.Equal(p.OrderInfo, roundTrip);
    }

    [Theory]
    [MemberData(nameof(TestOrder.GenerateValidOrders), MemberType = typeof(TestOrder))]
    public void OrderInfoRoundTripB(TestOrder.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestOrder.DataDirectory, p.JsonFile));
        var deserialized = _serializer.Deserialize<UnvalidatedOrderInfo>(json);
        var roundTrip = _serializer.Serialize(deserialized);
        Assert.Equal(json, roundTrip);
    }

    [Theory]
    [MemberData(nameof(TestOrder.GenerateValidOrders), MemberType = typeof(TestOrder))]
    public void OrderInfoDeserializeWorks(TestOrder.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestOrder.DataDirectory, p.JsonFile));
        var deserialized = _serializer.Deserialize<UnvalidatedOrderInfo>(json);
        Assert.Equal(p.OrderInfo, deserialized);
    }

    [Theory]
    [MemberData(nameof(TestOrder.GenerateValidOrders), MemberType = typeof(TestOrder))]
    public void OrderInfoSerializationWorks(TestOrder.ValidData p) {
        var json = File.ReadAllText(Path.Combine(TestOrder.DataDirectory, p.JsonFile));
        var serialized = _serializer.Serialize(p.OrderInfo);
        Assert.Equal(json, serialized);
    }

    [Fact]
    public void CheckCouponSerialization() {
        List<Coupon> coupons = [ new("1234") ];
        var json = _serializer.Serialize(coupons, writeIndented: false);
        Assert.Equal("[{\"Code\":\"1234\",\"Status\":0,\"StatusItems\":[]}]", json);
    }

    [Fact]
    public void CheckSavedCouponSerialization() {
        List<SavedCoupon> coupons = [ new("1234") ];
        var json = _serializer.Serialize(coupons, writeIndented: false);
        Assert.Equal("[\"1234\"]", json);
    }
}
