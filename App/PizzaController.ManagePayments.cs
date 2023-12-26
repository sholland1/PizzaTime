using System.Diagnostics;
using Hollandsoft.PizzaTime;

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
            var choice = IsAffirmative(_terminalUI.Prompt("Try again? [Y/n]: "));
            return choice ? CreatePayment() : default;
        }, p => {
            _terminalUI.PrintLine("New payment information:");
            _terminalUI.PrintLine(p.Summarize());
            var paymentName = GetPaymentName();
            var shouldSave = IsAffirmative(_terminalUI.Prompt($"Save payment information ({paymentName})? [Y/n]: "));
            _terminalUI.Clear();

            if (shouldSave) {
                _repo.SavePayment(paymentName, p);
                _terminalUI.PrintLine("Payment saved.");
                return p;
            }
            _terminalUI.PrintLine("Payment not saved.");
            return default;
        });
    }

    private string GetPaymentName(string existingName = "") {
        string? paymentName = _terminalUI.PromptForEdit("Payment name: ", existingName);
        if (paymentName is null) {
            _terminalUI.PrintLine("No payment name entered. Try again.");
            return GetPaymentName(existingName);
        }

        if (!paymentName.IsValidName()) {
            _terminalUI.PrintLine("Invalid payment name. Try again.");
            return GetPaymentName(existingName);
        }

        if (_repo.ListPayments().Where(n => n != existingName).Contains(paymentName)) {
            _terminalUI.PrintLine($"Payment '{paymentName}' already exists. Try again.");
            return GetPaymentName(existingName);
        }

        _terminalUI.Clear();
        return paymentName;
    }

    public async Task CreatePaymentsMenu() {
        _terminalUI.PrintLine("--Manage Payments--");

        string[] options = [
            "1. Create new payment info",
            "q. Return"
        ];

        _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = _terminalUI.PromptKey("Choose an option: ");
        _terminalUI.Clear();

        switch (choice) {
            case '1': _ = CreatePayment(); await ManagePaymentsMenu(); break;
            case 'Q' or 'q': return;
            default:
                _terminalUI.PrintLine("Not a valid option. Try again.");
                await CreatePaymentsMenu();
                break;
        }
    }

    public async Task ManagePaymentsMenu() {
        if (!_repo.ListPayments().Any()) {
            await CreatePaymentsMenu();
            return;
        }

        _terminalUI.PrintLine("--Manage Payments--");

        string[] options = [
            "1. Create new payment info",
            "2. Edit existing payment info",
            "3. Delete existing payment info",
            "4. Rename existing payment info",
            "q. Return"
        ];
        _terminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = _terminalUI.PromptKey("Choose an option: ");
        _terminalUI.Clear();

        switch (choice) {
            case '1': _ = CreatePayment(); await ManagePaymentsMenu(); break;
            case '2': _ = EditPayment(); await ManagePaymentsMenu(); break;
            case '3': DeletePayment(); await ManagePaymentsMenu(); break;
            case '4': RenamePayment(); await ManagePaymentsMenu(); break;
            case 'Q' or 'q': return;
            default:
                _terminalUI.PrintLine("Not a valid option. Try again.");
                await ManagePaymentsMenu();
                break;
        }
    }

    private void RenamePayment() {
        var paymentName = _chooser.GetUserChoice(
            "Choose a payment to rename: ", _repo.ListPayments(), "payment");
        if (paymentName is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No payment selected.");
            return;
        }

        var newPaymentName = GetPaymentName(paymentName);
        if (paymentName == newPaymentName) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("Payment not renamed.");
            return;
        }
        _repo.RenamePayment(paymentName, newPaymentName);

        _terminalUI.Clear();
        _terminalUI.PrintLine($"Payment renamed to '{newPaymentName}'.");
    }

    public Payment? EditPayment() {
        var paymentName = _chooser.GetUserChoice(
            "Choose a payment to edit: ", _repo.ListPayments(), "payment");
        if (paymentName is null) {
            _terminalUI.Clear();
            _terminalUI.PrintLine("No payment selected.");
            return default;
        }

        var payWithCard = (PaymentInfo.PayWithCard?)_repo.GetPayment(paymentName)?.PaymentInfo;
        Debug.Assert(payWithCard is not null, "Payment not found.");

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
            var choice = IsAffirmative(_terminalUI.Prompt("Try again? [Y/n]: "));
            _terminalUI.Clear();
            return choice ? EditPayment() : default;
        }, p => {
            _terminalUI.PrintLine("Updated payment:");
            _terminalUI.PrintLine(p.Summarize());
            var shouldSave = IsAffirmative(_terminalUI.Prompt($"Save payment information ({paymentName})? [Y/n]: "));
            _terminalUI.Clear();

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
            _terminalUI.Clear();
            _terminalUI.PrintLine("No payment selected.");
            return;
        }

        var payment = _repo.GetPayment(paymentName);
        Debug.Assert(payment is not null, "Payment not found.");

        _terminalUI.PrintLine($"Deleting '{paymentName}' payment information:");
        _terminalUI.PrintLine(payment.Summarize());
        var shouldDelete = IsAffirmative(_terminalUI.Prompt($"Delete payment ({paymentName})? [Y/n]: "));
        _terminalUI.Clear();

        if (shouldDelete) {
            _repo.DeletePayment(paymentName);
            _terminalUI.PrintLine("Payment deleted.");
            return;
        }
        _terminalUI.PrintLine("Payment not deleted.");
    }
}
