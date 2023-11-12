using Hollandsoft.OrderPizza;

namespace Controllers;
public partial class PizzaController {
    public Payment? CreatePayment() {
        _terminalUI.PrintLine("Please enter your payment information.");
        var cardNumber = _terminalUI.Prompt("Card number: ") ?? "";
        var expiration = _terminalUI.Prompt("Expiration date (MM/YY): ") ?? "";
        var cvv = _terminalUI.Prompt("CVV: ") ?? "";
        var zip = _terminalUI.Prompt("Billing zip code: ") ?? "";

        var payment = new UnvalidatedPayment(
            new PaymentInfo.PayWithCard(
                cardNumber, expiration, cvv, zip)).Parse();

        return payment.Match(es => {
            _terminalUI.PrintLine("Failed to parse payment info:");
            _terminalUI.PrintLine(string.Join(Environment.NewLine, es.Select(e => e.ErrorMessage)));
            var choice = _terminalUI.Prompt("Try again? [Y/n]: ");
            return IsAffirmative(choice) ? CreatePayment() : default;
        }, p => {
            _terminalUI.PrintLine("New payment information:");
            _terminalUI.PrintLine(p.Summarize());
            var paymentName = _terminalUI.Prompt("Payment name: ") ?? "";
            var shouldSave = IsAffirmative(_terminalUI.Prompt($"Save payment information ({paymentName})? [Y/n]: "));
            if (shouldSave) {
                _repo.SavePayment(paymentName, p);
                _terminalUI.PrintLine("Payment saved.");
                return p;
            }
            _terminalUI.PrintLine("Payment not saved.");
            return default;
        });
    }

    public async Task ManagePayments() {
        string[] options = new[] {
            "1. Create new payment info",
            "2. Edit existing payment info",
            "3. Delete existing payment info",
            "q. Return"
        };
        _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = _terminalUI.PromptKey("Choose an option: ");

        switch (choice) {
            case '1': _ = CreatePayment(); await ManagePayments(); break;
            case '2': _ = EditPayment(); await ManagePayments(); break;
            case '3': DeletePayment(); await ManagePayments(); break;
            case 'Q' or 'q': return;
            default:
                _terminalUI.PrintLine("Not a valid option. Try again.");
                await ManagePayments();
                break;
        }
    }

    public Payment? EditPayment() {
        var paymentName = _chooser.GetUserChoice(
            "Choose a payment to edit: ", _repo.ListPayments(), "payment");
        if (paymentName is null) {
            _terminalUI.PrintLine("No payment selected.");
            return default;
        }

        var payWithCard = (PaymentInfo.PayWithCard?)_repo.GetPayment(paymentName)?.PaymentInfo
            ?? throw new Exception("Payment not found.");

        _terminalUI.PrintLine($"Editing '{paymentName}' payment information:");
        var cardNumber = _terminalUI.PromptForEdit("Card number: ", payWithCard.CardNumber) ?? "";
        var expiration = _terminalUI.PromptForEdit("Expiration date (MM/YY): ", payWithCard.Expiration) ?? "";
        var cvv = _terminalUI.PromptForEdit("CVV: ", payWithCard.SecurityCode) ?? "";
        var zip = _terminalUI.PromptForEdit("Billing zip code: ", payWithCard.BillingZip) ?? "";

        var payment = new UnvalidatedPayment(
            new PaymentInfo.PayWithCard(
                cardNumber, expiration, cvv, zip)).Parse();

        return payment.Match(es => {
            _terminalUI.PrintLine("Failed to parse payment info:");
            _terminalUI.PrintLine(string.Join(Environment.NewLine, es.Select(e => e.ErrorMessage)));
            var choice = _terminalUI.Prompt("Try again? [Y/n]: ");
            return IsAffirmative(choice) ? EditPayment() : default;
        }, p => {
            _terminalUI.PrintLine("Updated payment:");
            _terminalUI.PrintLine(p.Summarize());
            var shouldSave = IsAffirmative(_terminalUI.Prompt($"Save payment information ({paymentName})? [Y/n]: "));
            if (shouldSave) {
                _repo.SavePayment(paymentName, p);
                _terminalUI.PrintLine("Payment saved.");
                return p;
            }
            _terminalUI.PrintLine("Payment not saved.");
            return default;
        });
    }

    public void DeletePayment() {
        var paymentName = _chooser.GetUserChoice(
            "Choose a payment to delete: ", _repo.ListPayments(), "payment");
        if (paymentName is null) {
            _terminalUI.PrintLine("No payment selected.");
            return;
        }

        var payment = _repo.GetPayment(paymentName) ?? throw new Exception("Payment not found.");
        _terminalUI.PrintLine($"Deleting '{paymentName}' payment information:");
        _terminalUI.PrintLine(payment.Summarize());
        var shouldDelete = IsAffirmative(_terminalUI.Prompt($"Delete payment ({paymentName})? [Y/n]: "));
        if (shouldDelete) {
            _repo.DeletePayment(paymentName);
            _terminalUI.PrintLine("Payment deleted.");
            return;
        }
        _terminalUI.PrintLine("Payment not deleted.");
    }
}
