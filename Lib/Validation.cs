using FluentValidation;
using FluentValidation.Results;
using static BuilderHelpers;

public class PizzaValidator : AbstractValidator<Pizza> {
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
        RuleFor(p => p.DippingSauce.GarlicAmount).InclusiveBetween(0, 25);
        RuleFor(p => p.DippingSauce.RanchAmount).InclusiveBetween(0, 25);
        RuleFor(p => p.DippingSauce.MarinaraAmount).InclusiveBetween(0, 25);
        RuleFor(p => p.Quantity).InclusiveBetween(1, 25);
    }
}

public static class ValidationHelpers {
    public static Result<List<ValidationFailure>, ValidPizza> Parse(this Pizza pizza) {
        PizzaValidator validator = new();
        var result = validator.Validate(pizza);
        return result.IsValid
            ? new Result<List<ValidationFailure>, ValidPizza>.Success(new(pizza))
            : new Result<List<ValidationFailure>, ValidPizza>.Failure(result.Errors);
    }
}

public class ValidPizza : Pizza {
    internal ValidPizza(Pizza pizza) : base(pizza) {}
}

public class OrderInfoValidator : AbstractValidator<OrderInfo> {
    public OrderInfoValidator() {
        When(o => o.ServiceMethod is ServiceMethod.Delivery,
            () => {
                RuleFor(o => ((ServiceMethod.Delivery)o.ServiceMethod).Address.State)
                    .Matches("[A-Z][A-Z]");
                RuleFor(o => ((ServiceMethod.Delivery)o.ServiceMethod).Address.ZipCode)
                    .Matches("\\d{5}");
            });
    }
}

public class PaymentInfoValidator : AbstractValidator<PaymentInfo> {
    public PaymentInfoValidator() {
        RuleFor(p => p.Email).EmailAddress();
        RuleFor(p => p.Phone).Matches("\\d{9}");

        When(p => p.Payment is Payment.PayWithCard,
            () => {
                RuleFor(p => ((Payment.PayWithCard)p.Payment).CardNumber.ToString()).CreditCard();
                RuleFor(p => ((Payment.PayWithCard)p.Payment).Expiration).Matches(@"[01]\d/\d\d");
                RuleFor(p => ((Payment.PayWithCard)p.Payment).SecurityCode).Matches("\\d{3}");
                RuleFor(p => ((Payment.PayWithCard)p.Payment).BillingZip).Matches("\\d{5}");
            });
    }
}
