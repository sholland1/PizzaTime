using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using static Hollandsoft.PizzaTime.PaymentInfo;

namespace Hollandsoft.PizzaTime;
public class UnvalidatedOrderInfo {
    public required string StoreId { get; init; }
    public required ServiceMethod ServiceMethod { get; init; }
    public required OrderTiming Timing { get; init; }

    public UnvalidatedOrderInfo() { }

    [SetsRequiredMembers]
    public UnvalidatedOrderInfo(string storeId, ServiceMethod serviceMethod, OrderTiming timing) {
        StoreId = storeId;
        ServiceMethod = serviceMethod;
        Timing = timing;
    }

    [SetsRequiredMembers]
    public UnvalidatedOrderInfo(UnvalidatedOrderInfo o) :
        this(o.StoreId, o.ServiceMethod, o.Timing) { }

    public static bool operator ==(UnvalidatedOrderInfo? a, UnvalidatedOrderInfo? b) => Equals(a, b);
    public static bool operator !=(UnvalidatedOrderInfo? a, UnvalidatedOrderInfo? b) => !Equals(a, b);

    public override bool Equals(object? obj) =>
        obj is UnvalidatedOrderInfo o
        && StoreId == o.StoreId
        && ServiceMethod == o.ServiceMethod
        && Timing == o.Timing;

    public override int GetHashCode() {
        HashCode hash = new();
        hash.Add(StoreId);
        hash.Add(ServiceMethod);
        hash.Add(Timing);
        return hash.ToHashCode();
    }
}

public abstract record ServiceMethod {
    public T Match<T>(Func<Address, T> delivery, Func<PickupLocation, T> carryout) => this switch {
        Delivery d => delivery(d.Address),
        Carryout c => carryout(c.Location),
        _ => throw new UnreachableException($"Invalid ServiceMethod! {this}")
    };

    public void Match(Action<Address> delivery, Action<PickupLocation> carryout) {
        switch (this) {
            case Delivery d: delivery(d.Address); break;
            case Carryout c: carryout(c.Location); break;
            default: throw new UnreachableException($"Invalid ServiceMethod! {this}");
        }
    }

    public abstract string Name { get; }

    public sealed record Delivery(Address Address) : ServiceMethod {
        public override string Name => "Delivery";
    }
    public sealed record Carryout(PickupLocation Location) : ServiceMethod {
        public override string Name => "Carryout";
    }
}

public record Address(
    AddressType AddressType, string? Name,
    string StreetAddress, int? Apt, string ZipCode,
    string City, string State);

public enum AddressType { House, Apartment, Business, Hotel, Other }

public enum PickupLocation { InStore, DriveThru, Carside }

public abstract record OrderTiming {
    public T Match<T>(Func<T> now, Func<DateTime, T> later) => this switch {
        Now => now(),
        Later l => later(l.DateTime),
        _ => throw new UnreachableException($"Invalid OrderTiming! {this}")
    };

    public void Match(Action now, Action<DateTime> later) {
        switch (this) {
            case Now: now(); break;
            case Later l: later(l.DateTime); break;
            default: throw new UnreachableException($"Invalid OrderTiming! {this}");
        }
    }

    public sealed record Now : OrderTiming {
        private Now() { }
        public static Now Instance { get; } = new();
    }

    public sealed record Later(DateTime DateTime) : OrderTiming;
}

public class UnvalidatedPersonalInfo {
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }

    public UnvalidatedPersonalInfo() { }

    [SetsRequiredMembers]
    public UnvalidatedPersonalInfo(string firstName, string lastName, string email, string phone) {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
    }

    [SetsRequiredMembers]
    public UnvalidatedPersonalInfo(UnvalidatedPersonalInfo p) :
        this(p.FirstName, p.LastName, p.Email, p.Phone) { }

    public static bool operator ==(UnvalidatedPersonalInfo? a, UnvalidatedPersonalInfo? b) => Equals(a, b);
    public static bool operator !=(UnvalidatedPersonalInfo? a, UnvalidatedPersonalInfo? b) => !Equals(a, b);

    public override bool Equals(object? obj) =>
        obj is UnvalidatedPersonalInfo p
        && FirstName == p.FirstName
        && LastName == p.LastName
        && Email == p.Email
        && Phone == p.Phone;

    public override int GetHashCode() {
        HashCode hash = new();
        hash.Add(FirstName);
        hash.Add(LastName);
        hash.Add(Email);
        hash.Add(Phone);
        return hash.ToHashCode();
    }
}

public enum PaymentType { PayAtStore, PayWithCard }

public class ActualOrder : UnvalidatedActualOrder {
    [SetsRequiredMembers]
    internal ActualOrder(UnvalidatedActualOrder order) {
        Pizzas = order.Pizzas;
        Coupons = order.Coupons;
        OrderInfo = order.OrderInfo;
        Payment = order.Payment;
    }

    public UnvalidatedHistoricalOrder ToHistOrder() => new() {
        Pizzas = [.. Pizzas.Select(p => new UnvalidatedPizza(p))],
        Coupons = Coupons,
        OrderInfo = OrderInfo,
        Payment = Payment
    };
}

public class UnvalidatedHistoricalOrder {
    public List<UnvalidatedPizza> Pizzas { get; init; } = [];
    public required List<Coupon> Coupons { get; init; }
    public required UnvalidatedOrderInfo OrderInfo { get; init; }
    public required Payment Payment { get; init; }
}

public record NamedOrder(string Name, ActualOrder Order);

public record OrderInstance(string Name, DateTime TimeStamp) {
    public OrderInstance(string displayString) : this("", new(0)) {
        var (timestamp, name) = Utils.SplitAtFirst(displayString, '-');
        Name = name.Trim();
        TimeStamp = DateTime.Parse(timestamp.Trim());
    }

    public override string ToString() => $"{TimeStamp:MM/dd/yyyy hh:mm:ss tt} - {Name}";
}

public class PastOrder {
    public required string OrderName { get; init; }
    public DateTime TimeStamp { get; init; }
    public required UnvalidatedHistoricalOrder Order { get; init; }
    public required string EstimatedWaitMinutes { get; init; }
    public decimal TotalPrice { get; init; }

    public OrderInstance ToOrderInstance() => new(OrderName, TimeStamp);
}

public class UnvalidatedActualOrder {
    public List<Pizza> Pizzas { get; init; } = [];
    public required List<Coupon> Coupons { get; init; }
    public required OrderInfo OrderInfo { get; init; }
    public required Payment Payment { get; init; }
}

public class SavedOrder {
    public List<SavedPizza> Pizzas { get; init; } = [];
    public required List<Coupon> Coupons { get; init; }
    public required UnvalidatedOrderInfo OrderInfo { get; init; }
    public required PaymentType PaymentType { get; init; }
    public string? PaymentInfoName { get; init; }

    public SavedOrder WithCoupons(List<Coupon> coupons) =>
        new() {
            Pizzas = Pizzas,
            Coupons = coupons,
            OrderInfo = OrderInfo,
            PaymentType = PaymentType,
            PaymentInfoName = PaymentInfoName
        };

    public SavedOrder WithOrderInfo(OrderInfo? orderInfo) =>
        orderInfo is null ? this : new() {
            Pizzas = Pizzas,
            Coupons = Coupons,
            OrderInfo = orderInfo,
            PaymentType = PaymentType,
            PaymentInfoName = PaymentInfoName
        };

    public SavedOrder WithPayment((PaymentType type, string? infoName)? payment) =>
        payment is not {} x ? this : new() {
            Pizzas = Pizzas,
            Coupons = Coupons,
            OrderInfo = OrderInfo,
            PaymentType = x.type,
            PaymentInfoName = x.infoName
        };

    public SavedOrder WithPizzas(List<SavedPizza> pizzas) =>
        pizzas.Count == 0 ? this : new() {
            Pizzas = pizzas,
            Coupons = Coupons,
            OrderInfo = OrderInfo,
            PaymentType = PaymentType,
            PaymentInfoName = PaymentInfoName
        };
}

public record SavedPizza(string Name, int Quantity);

public record UnvalidatedPayment(PaymentInfo PaymentInfo) {
    public T Match<T>(Func<T> store, Func<PayWithCard, T> card) => PaymentInfo switch {
        PayAtStore => store(),
        PayWithCard c => card(c),
        _ => throw new UnreachableException($"Invalid Payment! {PaymentInfo}")
    };

    public void Match(Action store, Action<PayWithCard> card) {
        switch (PaymentInfo) {
            case PayAtStore: store(); break;
            case PayWithCard c: card(c); break;
            default: throw new UnreachableException($"Invalid Payment! {PaymentInfo}");
        }
    }
}

public record Payment : UnvalidatedPayment {
    internal Payment(PaymentInfo PaymentInfo) : base(PaymentInfo) { }
    public static Payment PayAtStoreInstance => new(PayAtStore.Instance);
}

public abstract record PaymentInfo {
    public sealed record PayAtStore : PaymentInfo {
        private PayAtStore() { }
        public static PaymentInfo Instance => new PayAtStore();
    }

    public sealed record PayWithCard(
        string CardNumber, string Expiration,
        string SecurityCode, string BillingZip) : PaymentInfo {
        public string Type => GetCardType(CardNumber);
    }

    //https://cache.dominos.com/olo/6_118_2/assets/build/js/site/base-site.js
    protected static string GetCardType(string s) {
        if (Regex.IsMatch(s, @"^5[1-5]"))
            return "MASTERCARD";
        if (Regex.IsMatch(s, @"^6(?:011|5)"))
            return "DISCOVER";
        if (Regex.IsMatch(s, @"^5[06-9]|^6\d"))
            return "MAESTRO";
        if (Regex.IsMatch(s, @"^4"))
            return "VISA";
        if (Regex.IsMatch(s, @"^374622"))
            return "OPTIMA";
        if (Regex.IsMatch(s, @"^3[47]"))
            return "AMEX";
        if (Regex.IsMatch(s, @"^(?:2131|1800|35)"))
            return "JCB";
        if (Regex.IsMatch(s, @"^3(?:0[0-5]|[68])"))
            return "DINERS";
        if (Regex.IsMatch(s, @"^(?:5[1-5]\d{2}|222[1-9]|22[3-9]\d|2[3-6]\d{2}|27[01]\d|2720)"))
            return "MASTERCARD";
        return "UNKNOWN";
    }
}
