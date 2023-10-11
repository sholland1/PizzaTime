using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using static BuilderHelpers;

public class PizzaValidator : AbstractValidator<UnvalidatedPizza> {
    public PizzaValidator() {
        //Enums
        RuleFor(p => p.Size).IsInEnum();
        RuleFor(p => p.Crust).IsInEnum();
        When(p => p.Cheese is Cheese.Full,
            () => RuleFor(p => ((Cheese.Full)p.Cheese).Amount).IsInEnum());
        When(p => p.Cheese is Cheese.Sides,
            () => {
                When(p => ((Cheese.Sides)p.Cheese).Left.HasValue,
                    () => RuleFor(p => ((Cheese.Sides)p.Cheese).Left).IsInEnum());
                When(p => ((Cheese.Sides)p.Cheese).Right.HasValue,
                    () => RuleFor(p => ((Cheese.Sides)p.Cheese).Right).IsInEnum());
            });
        When(p => p.Sauce.HasValue,
            () => {
                RuleFor(p => p.Sauce!.Value.Amount).IsInEnum();
                RuleFor(p => p.Sauce!.Value.SauceType).IsInEnum();
            });
        RuleForEach(p => p.Toppings).ChildRules(ts => {
            ts.RuleFor(t => t.ToppingType).IsInEnum();
            ts.RuleFor(t => t.Amount).IsInEnum();
            ts.RuleFor(t => t.Location).IsInEnum();
        });
        RuleFor(p => p.Cut).IsInEnum();
        RuleFor(p => p.Bake).IsInEnum();

        //Size
        When(p => p.Size == Size.Small,
            () => RuleFor(p => p.Crust)
                .Must(c => c.In(Crust.HandTossed, Crust.Thin, Crust.GlutenFree)));
        When(p => p.Size == Size.Medium,
            () => RuleFor(p => p.Crust)
                .Must(c => c.In(Crust.HandTossed, Crust.Thin, Crust.HandmadePan)));
        When(p => p.Size == Size.Large,
            () => RuleFor(p => p.Crust)
                .Must(c => c.In(Crust.HandTossed, Crust.Thin, Crust.Brooklyn)));
        When(p => p.Size == Size.XL,
            () => RuleFor(p => p.Crust)
                .Must(c => c == Crust.Brooklyn));

        //Crust
        When(p => p.Crust == Crust.HandTossed,
            () => RuleFor(p => p.Oregano).Equal(false));
        When(p => p.Crust == Crust.Thin,
            () => {
                RuleFor(p => p.GarlicCrust).Equal(false);
                RuleFor(p => p.Bake).Equal(Bake.Normal);
            });
        When(p => p.Crust.In(Crust.HandmadePan, Crust.Brooklyn, Crust.GlutenFree),
            () => {
                RuleFor(p => p.GarlicCrust).Equal(false);
                RuleFor(p => p.Oregano).Equal(false);
            });
        When(p => p.Crust.In(Crust.HandmadePan, Crust.Brooklyn),
            () => {
                RuleFor(p => p.Cheese).Must(c => c is not Cheese.None);
                When(p => p.Cheese is Cheese.Sides,
                    () => {
                        RuleFor(p => ((Cheese.Sides)p.Cheese).Left).NotNull();
                        RuleFor(p => ((Cheese.Sides)p.Cheese).Right).NotNull();
                    });
            });
        When(p => p.Crust == Crust.HandmadePan && p.Sauce.HasValue,
            () => RuleFor(p => p.Sauce!.Value.SauceType).NotEqual(SauceType.Marinara));

        //Other
        RuleFor(p => p.Toppings)
            .Must(ts => ts
                .Where(t => t.Location.In(All, Left))
                .Sum(t => t.Amount == Extra ? 2 : 1)
                .Between(0, 10))
            .Must(ts => ts
                .Where(t => t.Location.In(All, Right))
                .Sum(t => t.Amount == Extra ? 2 : 1)
                .Between(0, 10))
            .Must(ts => !ts.Select(t => t.ToppingType).ContainsDuplicates());
        RuleFor(p => p.Quantity).InclusiveBetween(1, 25);
    }
}

public static class ValidationHelpers {
    // public static Result<List<ValidationFailure>, ValidPizza> Parse(this Pizza pizza) {
    //     PizzaValidator validator = new();
    //     var result = validator.Validate(pizza);
    //     return result.IsValid
    //         ? new Result<List<ValidationFailure>, ValidPizza>.Success(new(pizza))
    //         : new Result<List<ValidationFailure>, ValidPizza>.Failure(result.Errors);
    // }

    // public static Result<List<ValidationFailure>, ValidOrderInfo> Parse(this OrderInfo orderInfo) {
    //     OrderInfoValidator validator = new();
    //     var result = validator.Validate(orderInfo);
    //     return result.IsValid
    //         ? new Result<List<ValidationFailure>, ValidOrderInfo>.Success(new(orderInfo))
    //         : new Result<List<ValidationFailure>, ValidOrderInfo>.Failure(result.Errors);
    // }

    // public static Result<List<ValidationFailure>, ValidPaymentInfo> Parse(this PaymentInfo paymentInfo) {
    //     PaymentInfoValidator validator = new();
    //     var result = validator.Validate(paymentInfo);
    //     return result.IsValid
    //         ? new Result<List<ValidationFailure>, ValidPaymentInfo>.Success(new(paymentInfo))
    //         : new Result<List<ValidationFailure>, ValidPaymentInfo>.Failure(result.Errors);
    // }

    public static Pizza Validate(this UnvalidatedPizza pizza) {
        PizzaValidator validator = new();
        validator.ValidateAndThrow(pizza);
        return new(pizza);
    }

    public static NewOrder Validate(this UnvalidatedOrder order) {
        OrderValidator validator = new();
        validator.ValidateAndThrow(order);
        return ConvertToValidOrder(order);
    }

    private static NewOrder ConvertToValidOrder(UnvalidatedOrder order) {
        var validatedPizzas = order.Pizzas.Select(p => new Pizza(p)).ToList();
        return new(validatedPizzas, order.Coupons, new(order.OrderInfo), order.PaymentType);
    }

    public static OrderInfo Validate(this UnvalidatedOrderInfo orderInfo) {
        OrderInfoValidator validator = new();
        validator.ValidateAndThrow(orderInfo);
        return new(orderInfo);
    }

    public static PaymentInfo Validate(this UnvalidatedPaymentInfo paymentInfo) {
        PaymentInfoValidator validator = new();
        validator.ValidateAndThrow(paymentInfo);
        return paymentInfo.Match(
            () => PaymentInfo.PayAtStoreInstance,
            p => new PaymentInfo.ValidatedPayWithCard(p.CardNumber, p.Expiration, p.SecurityCode, p.BillingZip));
    }

    public static PersonalInfo Validate(this UnvalidatedPersonalInfo personalInfo) {
        PersonalInfoValidator validator = new();
        validator.ValidateAndThrow(personalInfo);
        return new(personalInfo);
    }

    public static Validation<Pizza> Parse(this UnvalidatedPizza pizza) {
        PizzaValidator validator = new();
        var result = validator.Validate(pizza);
        return result.IsValid
            ? new Validation<Pizza>.Success(new(pizza))
            : new Validation<Pizza>.Failure(result.Errors);
    }

    public static Validation<NewOrder> Parse(this UnvalidatedOrder order) {
        OrderValidator validator = new();
        var result = validator.Validate(order);

        return result.IsValid
            ? new Validation<NewOrder>.Success(ConvertToValidOrder(order))
            : new Validation<NewOrder>.Failure(result.Errors);
    }

    public static Validation<OrderInfo> Parse(this UnvalidatedOrderInfo orderInfo) {
        OrderInfoValidator validator = new();
        var result = validator.Validate(orderInfo);
        return result.IsValid
            ? new Validation<OrderInfo>.Success(new(orderInfo))
            : new Validation<OrderInfo>.Failure(result.Errors);
    }

    public static Validation<PaymentInfo> Parse(this UnvalidatedPaymentInfo paymentInfo) {
        PaymentInfoValidator validator = new();
        var result = validator.Validate(paymentInfo);
        return result.IsValid
            ? new Validation<PaymentInfo>.Success(paymentInfo.Match(
                () => PaymentInfo.PayAtStoreInstance,
                p => new PaymentInfo.ValidatedPayWithCard(p.CardNumber, p.Expiration, p.SecurityCode, p.BillingZip)))
            : new Validation<PaymentInfo>.Failure(result.Errors);
    }

    public static Validation<PersonalInfo> Parse(this UnvalidatedPersonalInfo personalInfo) {
        PersonalInfoValidator validator = new();
        var result = validator.Validate(personalInfo);
        return result.IsValid
            ? new Validation<PersonalInfo>.Success(new(personalInfo))
            : new Validation<PersonalInfo>.Failure(result.Errors);
    }
}

public class PersonalInfo : UnvalidatedPersonalInfo {
    [SetsRequiredMembers]
    internal PersonalInfo(UnvalidatedPersonalInfo personalInfo) : base(personalInfo) { }
}

public record PaymentInfo : UnvalidatedPaymentInfo {

    public sealed record ValidatedPayAtStore : PaymentInfo {
        public static implicit operator PayAtStore(ValidatedPayAtStore p) => new();
    }

    public sealed record ValidatedPayWithCard(
        string CardNumber, string Expiration,
        string SecurityCode, string BillingZip) : PaymentInfo {
        public string Type => GetCardType(CardNumber);

        public static implicit operator PayWithCard(ValidatedPayWithCard p) =>
            new(p.CardNumber, p.Expiration, p.SecurityCode, p.BillingZip);
    }

    public static PaymentInfo PayAtStoreInstance { get; } = new ValidatedPayAtStore();

    public T Match<T>(Func<T> store, Func<ValidatedPayWithCard, T> card) => this switch {
        ValidatedPayAtStore => store(),
        ValidatedPayWithCard c => card(c),
        _ => throw new UnreachableException($"Invalid Payment! {this}")
    };

    public void Match(Action store, Action<ValidatedPayWithCard> card) {
        switch (this) {
            case ValidatedPayAtStore: store(); break;
            case ValidatedPayWithCard c: card(c); break;
            default: throw new UnreachableException($"Invalid Payment! {this}");
        }
    }
}

public class OrderInfo : UnvalidatedOrderInfo {
    [SetsRequiredMembers]
    internal OrderInfo(UnvalidatedOrderInfo orderInfo) : base(orderInfo) { }
}

public class NewOrder : UnvalidatedOrder {
    [SetsRequiredMembers]
    internal NewOrder(List<Pizza> pizzas, List<Coupon> coupons, OrderInfo orderInfo, PaymentType paymentType) {
        base.Pizzas = pizzas.OfType<UnvalidatedPizza>().ToList();
        base.OrderInfo = orderInfo;
        Coupons = coupons;
        PaymentType = paymentType;
        Pizzas = pizzas;
        OrderInfo = orderInfo;
    }
    public new List<Pizza> Pizzas { get; }
    public new OrderInfo OrderInfo { get; }
}

public class Pizza : UnvalidatedPizza {
    internal Pizza(UnvalidatedPizza pizza) : base(pizza) { }
}

internal class OrderValidator : AbstractValidator<UnvalidatedOrder> {
    public OrderValidator() {
        RuleFor(o => o.Pizzas).NotEmpty();
        RuleForEach(o => o.Pizzas).SetValidator(new PizzaValidator());
        RuleFor(o => o.OrderInfo).SetValidator(new OrderInfoValidator());
        RuleFor(o => o.PaymentType).IsInEnum();
    }
}

internal class OrderInfoValidator : AbstractValidator<UnvalidatedOrderInfo> {
    public OrderInfoValidator() {
        RuleFor(o => int.Parse(o.StoreId)).GreaterThanOrEqualTo(0).WithName("StoreId");
        When(o => o.ServiceMethod is ServiceMethod.Carryout,
            () => RuleFor(o => ((ServiceMethod.Carryout)o.ServiceMethod).Location).IsInEnum());
        When(o => o.ServiceMethod is ServiceMethod.Delivery,
            () => RuleFor(o => ((ServiceMethod.Delivery)o.ServiceMethod).Address).SetValidator(new AddressValidator()));
    }
}

internal class AddressValidator : AbstractValidator<Address> {
    public AddressValidator() {
        RuleFor(a => a.StreetAddress).Matches("^\\d+ ");
        RuleFor(a => a.AddressType).IsInEnum();
        RuleFor(a => a.Apt).GreaterThanOrEqualTo(0);
        RuleFor(a => a.State).Matches("^[A-Z]{2}$");
        RuleFor(a => a.ZipCode).Matches("^\\d{5}$");
    }
}

internal class PaymentInfoValidator : AbstractValidator<UnvalidatedPaymentInfo> {
    public PaymentInfoValidator() {
        When(p => p is UnvalidatedPaymentInfo.PayWithCard,
            () => RuleFor(p => (UnvalidatedPaymentInfo.PayWithCard)p).SetValidator(new PayWithCardValidator()));
    }
}

internal class PersonalInfoValidator : AbstractValidator<UnvalidatedPersonalInfo> {
    public PersonalInfoValidator() {
        RuleFor(p => p.Email).EmailAddress();
        RuleFor(p => p.Phone).Matches(@"\d{3}-\d{3}-\d{4}");
    }
}

internal class PayWithCardValidator : AbstractValidator<UnvalidatedPaymentInfo.PayWithCard> {
    public PayWithCardValidator() {
        RuleFor(p => p.CardNumber).CreditCard();
        RuleFor(p => p.Expiration).Must(Utils.MatchesMMyy);
        RuleFor(p => p.SecurityCode).Matches("^\\d{3}$");
        RuleFor(p => p.BillingZip).Matches("^\\d{5}$");
    }
}
