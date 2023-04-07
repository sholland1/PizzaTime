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
    public static Pizza Validate(this UnvalidatedPizza pizza) {
        PizzaValidator validator = new();
        validator.ValidateAndThrow(pizza);
        return new(pizza);
    }

    public static OrderInfo Validate(this UnvalidatedOrderInfo orderInfo) {
        OrderInfoValidator validator = new();
        validator.ValidateAndThrow(orderInfo);
        return new(orderInfo);
    }

    public static PaymentInfo Validate(this UnvalidatedPaymentInfo paymentInfo) {
        PaymentInfoValidator validator = new();
        validator.ValidateAndThrow(paymentInfo);
        return new(paymentInfo);
    }

    public static Validation<Pizza> Parse(this UnvalidatedPizza pizza) {
        PizzaValidator validator = new();
        var result = validator.Validate(pizza);
        return result.IsValid
            ? new Validation<Pizza>.Success(new(pizza))
            : new Validation<Pizza>.Failure(result.Errors);
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
            ? new Validation<PaymentInfo>.Success(new(paymentInfo))
            : new Validation<PaymentInfo>.Failure(result.Errors);
    }
}

public class PaymentInfo : UnvalidatedPaymentInfo {
    [SetsRequiredMembers]
    internal PaymentInfo(UnvalidatedPaymentInfo paymentInfo) : base(paymentInfo) { }
}

public class OrderInfo : UnvalidatedOrderInfo {
    [SetsRequiredMembers]
    internal OrderInfo(UnvalidatedOrderInfo orderInfo) : base(orderInfo) { }
}

public class Pizza : UnvalidatedPizza {
    internal Pizza(UnvalidatedPizza pizza) : base(pizza) {}
}

public class OrderInfoValidator : AbstractValidator<UnvalidatedOrderInfo> {
    public OrderInfoValidator() {
        RuleFor(o => o.StoreId).GreaterThanOrEqualTo(0);
        When(o => o.ServiceMethod is ServiceMethod.Carryout,
            () => RuleFor(o => ((ServiceMethod.Carryout)o.ServiceMethod).Location).IsInEnum());
        When(o => o.ServiceMethod is ServiceMethod.Delivery,
            () => RuleFor(o => ((ServiceMethod.Delivery)o.ServiceMethod).Address).SetValidator(new AddressValidator()));
    }
}

public class AddressValidator : AbstractValidator<Address> {
    public AddressValidator() {
        RuleFor(d => d.AddressType).IsInEnum();
        RuleFor(d => d.Apt).GreaterThanOrEqualTo(0);
        RuleFor(d => d.State).Matches("^[A-Z][A-Z]$");
        RuleFor(d => d.ZipCode).Matches("^\\d{5}$");
    }
}

public class PaymentInfoValidator : AbstractValidator<UnvalidatedPaymentInfo> {
    public PaymentInfoValidator() {
        RuleFor(p => p.Email).EmailAddress();
        RuleFor(p => p.Phone).Matches(@"\d{3}-\d{3}-\d{4}");

        When(p => p.Payment is Payment.PayWithCard,
            () => RuleFor(p => ((Payment.PayWithCard)p.Payment)).SetValidator(new PayWithCardValidator()));
    }
}

public class PayWithCardValidator : AbstractValidator<Payment.PayWithCard> {
    public PayWithCardValidator() {
        RuleFor(p => p.CardNumber.ToString())
            .CreditCard()
            .WithName("CardNumber");
        RuleFor(p => p.Expiration).Must(Utils.MatchesMMyy);
        RuleFor(p => p.SecurityCode).Matches("^\\d{3}$");
        RuleFor(p => p.BillingZip).Matches("^\\d{5}$");
    }
}