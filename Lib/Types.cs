//TODO: test order and payment validation
//TODO: stub out api calls
using System.Diagnostics;

public record OrderInfo(int StoreId, ServiceMethod ServiceMethod, OrderTiming Timing);

public abstract record ServiceMethod {
    public T Match<T>(Func<Delivery, T> f1, Func<Carryout, T> f2) => this switch {
        Delivery d => f1(d),
        Carryout c => f2(c),
        _ => throw new UnreachableException("Invalid ServiceMethod")
    };

    public void Match(Action<Delivery> f1, Action<Carryout> f2) {
        switch (this) {
            case Delivery d: f1(d); break;
            case Carryout c: f2(c); break;
            default: throw new UnreachableException("Invalid ServiceMethod");
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
    public T Match<T>(Func<T> f1, Func<DateTime, T> f2) => this switch {
        Now => f1(),
        Later l => f2(l.DateTime),
        _ => throw new UnreachableException("Invalid ServiceMethod")
    };

    public void Match(Action f1, Action<DateTime> f2) {
        switch (this) {
            case Now: f1(); break;
            case Later l: f2(l.DateTime); break;
            default: throw new UnreachableException("Invalid OrderTiming");
        }
    }

    public sealed record Now : OrderTiming;
    public sealed record Later(DateTime DateTime) : OrderTiming;
}

public record PaymentInfo(
    string FirstName, string LastName,
    string Email, string Phone, Payment Payment);

public abstract record Payment {
    public sealed record PayAtStore : Payment;
    public sealed record PayWithCard(
        long CardNumber, string Expiration,
        string SecurityCode, string BillingZip) : Payment;
}