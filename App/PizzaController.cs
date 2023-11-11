using Hollandsoft.OrderPizza;

namespace Controllers;
public class PizzaController {
    private readonly IPizzaRepo _repo;
    private readonly Func<OrderInfo, ICart> _startOrder;
    private readonly ITerminalUI _terminalUI;
    private readonly IUserChooser _chooser;
    private readonly IAIPizzaBuilder _aiPizzaBuilder;

    public PizzaController(IPizzaRepo repo, Func<OrderInfo, ICart> startOrder, ITerminalUI terminalUI, IUserChooser chooser, IAIPizzaBuilder aiPizzaBuilder) =>
        (_repo, _startOrder, _terminalUI, _chooser, _aiPizzaBuilder) = (repo, startOrder, terminalUI, chooser, aiPizzaBuilder);

    public PersonalInfo CreatePersonalInfo() {
        _terminalUI.PrintLine("Please enter your personal information.");
        var firstName = _terminalUI.Prompt("First name: ") ?? "";
        var lastName = _terminalUI.Prompt("Last name: ") ?? "";
        var email = _terminalUI.Prompt("Email: ") ?? "";
        var phone = _terminalUI.Prompt("Phone: ") ?? "";

        return ValidatePersonalAndSave(firstName, lastName, email, phone) ?? CreatePersonalInfo();
    }

    private PersonalInfo? ValidatePersonalAndSave(string firstName, string lastName, string email, string phone) {
        var personalInfo = new UnvalidatedPersonalInfo {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone
        }.Parse();

        return personalInfo.Match<PersonalInfo?>(es => {
            _terminalUI.PrintLine("Failed to parse personal info:");
            _terminalUI.PrintLine(string.Join(Environment.NewLine, es.Select(e => e.ErrorMessage)));
            return default;
        }, pi => {
            _repo.SavePersonalInfo(pi);
            _terminalUI.PrintLine("Personal info saved.");
            return pi;
        });
    }

    public Payment CreatePaymentInfo() {
        _terminalUI.PrintLine("Please enter your payment information.");
        var cardNumber = _terminalUI.Prompt("Card number: ") ?? "";
        var expiration = _terminalUI.Prompt("Expiration date (MM/YY): ") ?? "";
        var cvv = _terminalUI.Prompt("CVV: ") ?? "";
        var zip = _terminalUI.Prompt("Billing zip code: ") ?? "";

        return ValidatePaymentAndSave(cardNumber, expiration, cvv, zip) ?? CreatePaymentInfo();
    }

    private Payment? ValidatePaymentAndSave(string cardNumber, string expiration, string cvv, string zip) {
        var payment = new UnvalidatedPayment(
            new PaymentInfo.PayWithCard(
                cardNumber, expiration, cvv, zip)).Parse();

        return payment.Match<Payment?>(es => {
            _terminalUI.PrintLine("Failed to parse payment info:");
            _terminalUI.PrintLine(string.Join(Environment.NewLine, es.Select(e => e.ErrorMessage)));
            return default;
        }, p => {
            _repo.SavePayment("default", p);
            _terminalUI.PrintLine("Payment info saved.");
            return p;
        });
    }

    public Payment EditPaymentInfo() {
        var payWithCard = (PaymentInfo.PayWithCard?)_repo.GetDefaultPayment()?.PaymentInfo;
        if (payWithCard is null) {
            return CreatePaymentInfo();
        }

        _terminalUI.PrintLine("Edit your payment information:");
        var cardNumber = _terminalUI.PromptForEdit("Card number: ", payWithCard.CardNumber) ?? "";
        var expiration = _terminalUI.PromptForEdit("Expiration date (MM/YY): ", payWithCard.Expiration) ?? "";
        var cvv = _terminalUI.PromptForEdit("CVV: ", payWithCard.SecurityCode) ?? "";
        var zip = _terminalUI.PromptForEdit("Billing zip code: ", payWithCard.BillingZip) ?? "";

        return ValidatePaymentAndSave(cardNumber, expiration, cvv, zip) ?? EditPaymentInfo();
    }

    public async Task FastPizza() {
        var userOrder = _repo.GetDefaultOrder()
            ?? throw new NotImplementedException("Implement create order.");

        var personalInfo = _repo.GetPersonalInfo() ?? CreatePersonalInfo();

        var userPayment = userOrder.PaymentType == PaymentType.PayAtStore
            ? Payment.PayAtStoreInstance
            : _repo.GetDefaultPayment() ?? CreatePaymentInfo();

        var cart = _startOrder(userOrder.OrderInfo);

        bool firstTime = true;
        foreach (var pizza in userOrder.Pizzas) {
            var cartResult = await cart.AddPizza(pizza);
            cartResult.Match(
                _terminalUI.PrintLine,
                v => {
                    if (firstTime) {
                        firstTime = false;
                        _terminalUI.PrintLine($"Order ID: {v.OrderID}\n");
                    }

                    _terminalUI.PrintLine($"Pizza was added to cart. Product Count: {v.ProductCount}\n{pizza.Summarize()}\n");
                });
            if (cartResult.IsFailure) return;
        }

        foreach (var coupon in userOrder.Coupons) {
            cart.AddCoupon(coupon);
            _terminalUI.PrintLine($"Coupon {coupon.Code} was added to cart.");
        }
        if (userOrder.Coupons.Any()) {
            _terminalUI.PrintLine();
        }

        var priceResult = await cart.GetSummary();
        priceResult.Match(
            message => _terminalUI.PrintLine($"Failed to check cart price:\n{message}"),
            summary => _terminalUI.PrintLine(
                $"""
                Cart summary:
                {userOrder.OrderInfo.Summarize()}
                Estimated Wait: {summary.WaitTime}
                Price: ${summary.TotalPrice}

                {userPayment.Summarize()}

                """));
        if (priceResult.IsFailure) return;

        var answer = _terminalUI.Prompt("Confirm order? [Y/n]: ");
        _terminalUI.PrintLine();

        if (!IsAffirmative(answer)) {
            _terminalUI.PrintLine("Order cancelled.");
            return;
        }

        _terminalUI.PrintLine("Ordering pizza...");

        var orderResult = await cart.PlaceOrder(personalInfo, userPayment);
        _terminalUI.PrintLine(
            orderResult.Match(
                message => $"Failed to place order: {message}",
                message => $"Order summary:\n{message}\nDone."));
    }

    public async Task ShowOptions() {
        _terminalUI.PrintLine("Welcome to the pizza ordering app!🍕");
        await Helper();

        async Task Helper() {
            string[] options = {
                "1. Order default",
                "2. Start new order",
                "3. Edit saved pizza",
                "4. Edit personal info",
                "5. Edit payment info",
                "6. Track order",
                "q. Exit"
            };
            _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
            var choice = _terminalUI.PromptKey("Choose an option: ");
            // return _terminalUI.FuzzyChoice(options);

            switch (choice) {
                case '1': await FastPizza(); break;
                // case '2': await NewOrder(); break;
                case '3': await EditSavedPizza(); await Helper(); break;
                case '4': _ = EditPersonalInfo(); await Helper(); break;
                case '5': _ = EditPaymentInfo(); await Helper(); break;
                // case '6': await TrackOrder(); await Helper(); break;
                case 'Q' or 'q': _terminalUI.PrintLine("Goodbye!"); return;
                default:
                    _terminalUI.PrintLine("Not a valid option. Try again.");
                    await Helper();
                    break;
            }
        }
    }

    public async Task EditSavedPizza() {
        var pizzaName = _chooser.GetUserChoice("Choose a pizza to edit: ", _repo.ListPizzas(), "pizza");
        if (pizzaName is null) {
            _terminalUI.PrintLine("No pizza selected.");
            return;
        }
        var pizza = _repo.GetPizza(pizzaName);
        if (pizza is null) throw new Exception("Pizza not found.");

        _terminalUI.PrintLine($"Editing {pizzaName}:");
        _terminalUI.PrintLine(pizza.Summarize());
        var input = _terminalUI.Prompt("> ") ?? "";
        var result = await _aiPizzaBuilder.EditPizza(pizza, input);

        result.Match(es => {
            _terminalUI.PrintLine("Failed to edit pizza:");
            foreach (var e in es) {
                _terminalUI.PrintLine(e);
            }
        }, p => {
            _terminalUI.PrintLine("Updated pizza:");
            _terminalUI.PrintLine(p.Summarize());
            var shouldSave = IsAffirmative(_terminalUI.Prompt("Save pizza? [Y/n]: "));
            if (shouldSave) {
                _repo.SavePizza(pizzaName, p);
                _terminalUI.PrintLine("Pizza saved.");
                return;
            }
            _terminalUI.PrintLine("Pizza not saved.");
        });
    }

    public PersonalInfo EditPersonalInfo() {
        var currentInfo = _repo.GetPersonalInfo();
        if (currentInfo is null) {
            return CreatePersonalInfo();
        }

        _terminalUI.PrintLine("Edit your personal information:");
        var firstName = _terminalUI.PromptForEdit("First Name: ", currentInfo.FirstName) ?? "";
        var lastName = _terminalUI.PromptForEdit("Last Name: ", currentInfo.LastName) ?? "";
        var email = _terminalUI.PromptForEdit("Email: ", currentInfo.Email) ?? "";
        var phone = _terminalUI.PromptForEdit("Phone Number: ", currentInfo.Phone) ?? "";

        return ValidatePersonalAndSave(firstName, lastName, email, phone) ?? EditPersonalInfo();
    }

    private static bool IsAffirmative(string? answer) => (answer?.ToLower() ?? "y") == "y";
}
