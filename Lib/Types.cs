using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

public class UnvalidatedOrderInfo {
    public required string StoreId { get; init; }
    public required ServiceMethod ServiceMethod { get; init; }
    public required OrderTiming Timing { get; init; }

    public UnvalidatedOrderInfo() {}

    [SetsRequiredMembers]
    public UnvalidatedOrderInfo(string storeId, ServiceMethod serviceMethod, OrderTiming timing) {
        StoreId = storeId;
        ServiceMethod = serviceMethod;
        Timing = timing;
    }

    [SetsRequiredMembers]
    public UnvalidatedOrderInfo(UnvalidatedOrderInfo o) :
        this(o.StoreId, o.ServiceMethod, o.Timing) {}

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

    public sealed record Delivery(Address Address) : ServiceMethod;
    public sealed record Carryout(PickupLocation Location) : ServiceMethod;
}

public record Address(
    AddressType AddressType, string? Name,
    string StreetAddress, int? Apt, string ZipCode,
    string City, string State);

public enum AddressType { House, Apartment, Business, Hotel, Other }

public enum PickupLocation { InStore, Window, Carside }

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

    public sealed record Now : OrderTiming;
    public sealed record Later(DateTime DateTime) : OrderTiming;
}

public class UnvalidatedPaymentInfo {
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required Payment Payment { get; init; }

    public UnvalidatedPaymentInfo() {}

    [SetsRequiredMembers]
    public UnvalidatedPaymentInfo(string firstName, string lastName, string email, string phone, Payment payment) {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        Payment = payment;
    }

    [SetsRequiredMembers]
    public UnvalidatedPaymentInfo(UnvalidatedPaymentInfo p) :
        this(p.FirstName, p.LastName, p.Email, p.Phone, p.Payment) {}

    public static bool operator ==(UnvalidatedPaymentInfo? a, UnvalidatedPaymentInfo? b) => Equals(a, b);
    public static bool operator !=(UnvalidatedPaymentInfo? a, UnvalidatedPaymentInfo? b) => !Equals(a, b);

    public override bool Equals(object? obj) =>
        obj is UnvalidatedPaymentInfo p
        && FirstName == p.FirstName
        && LastName == p.LastName
        && Email == p.Email
        && Phone == p.Phone
        && Payment == p.Payment;

    public override int GetHashCode() {
        HashCode hash = new();
        hash.Add(FirstName);
        hash.Add(LastName);
        hash.Add(Email);
        hash.Add(Phone);
        hash.Add(Payment);
        return hash.ToHashCode();
    }
}

public abstract record Payment {
    public T Match<T>(Func<T> store, Func<PayWithCard, T> card) => this switch {
        PayAtStore => store(),
        PayWithCard c => card(c),
        _ => throw new UnreachableException($"Invalid Payment! {this}")
    };

    public void Match(Action store, Action<PayWithCard> card) {
        switch (this) {
            case PayAtStore: store(); break;
            case PayWithCard c: card(c); break;
            default: throw new UnreachableException($"Invalid Payment! {this}");
        }
    }

    public sealed record PayAtStore : Payment;
    public sealed record PayWithCard(
        string CardNumber, string Expiration,
        string SecurityCode, string BillingZip) : Payment {
        public string Type {
            //https://cache.dominos.com/olo/6_118_2/assets/build/js/site/base-site.js
            get {
                var s = CardNumber;
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
                return "";
            }
        }
    }
}
