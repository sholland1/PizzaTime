//TODO: test order and payment validation
//TODO: stub out api calls
using System.Diagnostics;

public record OrderInfo(int StoreId, ServiceMethod ServiceMethod, OrderTiming Timing);

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

public record PaymentInfo(
    string FirstName, string LastName,
    string Email, string Phone, Payment Payment);

public abstract record Payment {
    public T Match<T>(Func<T> store, Func<long, string, string, string, T> card) => this switch {
        PayAtStore => store(),
        PayWithCard c => card(c.CardNumber, c.Expiration, c.SecurityCode, c.BillingZip),
        _ => throw new UnreachableException($"Invalid Payment! {this}")
    };

    public void Match(Action store, Action<long, string, string, string> card) {
        switch (this) {
            case PayAtStore: store(); break;
            case PayWithCard c: card(c.CardNumber, c.Expiration, c.SecurityCode, c.BillingZip); break;
            default: throw new UnreachableException($"Invalid Payment! {this}");
        }
    }

    public sealed record PayAtStore : Payment;
    public sealed record PayWithCard(
        long CardNumber, string Expiration,
        string SecurityCode, string BillingZip) : Payment;
}