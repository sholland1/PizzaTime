using System.Data;
using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using static Hollandsoft.OrderPizza.BuilderHelpers;

namespace Hollandsoft.OrderPizza;
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

        //Size and Crust
        RuleFor(p => p.Crust).Must((p, c) => p.Size.AllowedCrusts().Contains(c));

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

    public static Payment Validate(this UnvalidatedPayment payment) {
        PaymentValidator validator = new();
        validator.ValidateAndThrow(payment);
        return new(payment.PaymentInfo);
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

    public static Validation<OrderInfo> Parse(this UnvalidatedOrderInfo orderInfo) {
        OrderInfoValidator validator = new();
        var result = validator.Validate(orderInfo);
        return result.IsValid
            ? new Validation<OrderInfo>.Success(new(orderInfo))
            : new Validation<OrderInfo>.Failure(result.Errors);
    }

    public static Validation<Payment> Parse(this UnvalidatedPayment payment) {
        PaymentValidator validator = new();
        var result = validator.Validate(payment);
        return result.IsValid
            ? new Validation<Payment>.Success(new(payment.PaymentInfo))
            : new Validation<Payment>.Failure(result.Errors);
    }

    public static Validation<PersonalInfo> Parse(this UnvalidatedPersonalInfo personalInfo) {
        PersonalInfoValidator validator = new();
        var result = validator.Validate(personalInfo);
        return result.IsValid
            ? new Validation<PersonalInfo>.Success(new(personalInfo))
            : new Validation<PersonalInfo>.Failure(result.Errors);
    }

    public static ActualOrder Validate(this UnvalidatedActualOrder order) {
        ActualOrderValidator validator = new();
        validator.ValidateAndThrow(order);
        return new(order);
    }

    public static Validation<ActualOrder> Parse(this UnvalidatedActualOrder order) {
        ActualOrderValidator validator = new();
        var result = validator.Validate(order);
        return result.IsValid
            ? new Validation<ActualOrder>.Success(new(order))
            : new Validation<ActualOrder>.Failure(result.Errors);
    }
}

public class PersonalInfo : UnvalidatedPersonalInfo {
    [SetsRequiredMembers]
    internal PersonalInfo(UnvalidatedPersonalInfo personalInfo) : base(personalInfo) { }
}

public class OrderInfo : UnvalidatedOrderInfo {
    [SetsRequiredMembers]
    internal OrderInfo(UnvalidatedOrderInfo orderInfo) : base(orderInfo) { }
}

public class Pizza : UnvalidatedPizza {
    internal Pizza(UnvalidatedPizza pizza) : base(pizza) { }
    public Pizza WithQuantity(int quantity) => new(this) { Quantity = Math.Max(1, quantity) };
}

internal class ActualOrderValidator : AbstractValidator<UnvalidatedActualOrder> {
    public ActualOrderValidator() {
        RuleFor(o => o.Pizzas).NotEmpty();
        //TODO: don't know if this is true (shouldn't be PayAtStore though)
        When(o => o.OrderInfo.ServiceMethod is ServiceMethod.Delivery,
            () => RuleFor(o => o.Payment).Must(p => p.PaymentInfo is PaymentInfo.PayWithCard));
    }
}

internal class OrderInfoValidator : AbstractValidator<UnvalidatedOrderInfo> {
    public OrderInfoValidator() {
        RuleFor(o => int.Parse(o.StoreId)).GreaterThanOrEqualTo(0).WithName("StoreId");
        When(o => o.ServiceMethod is ServiceMethod.Carryout,
            () => RuleFor(o => ((ServiceMethod.Carryout)o.ServiceMethod).Location).IsInEnum());
        When(o => o.ServiceMethod is ServiceMethod.Delivery,
            () => RuleFor(o => ((ServiceMethod.Delivery)o.ServiceMethod).Address).SetValidator(new AddressValidator()));
        When(o => o.Timing is OrderTiming.Later,
            () => RuleFor(o => ((OrderTiming.Later)o.Timing).DateTime)
                    .Must(d => d.Minute.In(0, 15, 30, 45)
                                && d.Second == 0
                                && d.Millisecond == 0));
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

internal class PaymentValidator : AbstractValidator<UnvalidatedPayment> {
    public PaymentValidator() {
        When(p => p.PaymentInfo is PaymentInfo.PayWithCard,
            () => RuleFor(p => (PaymentInfo.PayWithCard)p.PaymentInfo).SetValidator(new PayWithCardValidator()));
    }
}

internal class PersonalInfoValidator : AbstractValidator<UnvalidatedPersonalInfo> {
    public PersonalInfoValidator() {
        RuleFor(p => p.Email).EmailAddress();
        RuleFor(p => p.Phone).Matches(@"^\d{3}-\d{3}-\d{4}$");
    }
}

internal class PayWithCardValidator : AbstractValidator<PaymentInfo.PayWithCard> {
    public PayWithCardValidator() {
        RuleFor(p => p.CardNumber).CreditCard();
        RuleFor(p => p.Expiration).Must(Utils.MatchesMMyy);
        RuleFor(p => p.SecurityCode).Matches("^\\d{3}$");
        RuleFor(p => p.BillingZip).Matches("^\\d{5}$");
    }
}
