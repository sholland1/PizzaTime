using Hollandsoft.OrderPizza;

namespace Controllers;
public partial class PizzaController {
    public Payment? CreatePayment() {
        TerminalUI.PrintLine("Please enter your payment information.");
        var cardNumber = TerminalUI.Prompt("Card number: ") ?? "";
        var expiration = TerminalUI.Prompt("Expiration date (MM/YY): ") ?? "";
        var cvv = TerminalUI.Prompt("CVV: ") ?? "";
        var zip = TerminalUI.Prompt("Billing zip code: ") ?? "";

        var payment = new UnvalidatedPayment(
            new PaymentInfo.PayWithCard(
                cardNumber, expiration, cvv, zip)).Parse();

        return payment.Match(es => {
            TerminalUI.PrintLine("Failed to parse payment info:");
            TerminalUI.PrintLine(string.Join(Environment.NewLine, es.Select(e => e.ErrorMessage)));
            var choice = IsAffirmative(TerminalUI.Prompt("Try again? [Y/n]: "));
            return choice ? CreatePayment() : default;
        }, p => {
            TerminalUI.PrintLine("New payment information:");
            TerminalUI.PrintLine(p.Summarize());
            var paymentName = GetPaymentName();
            var shouldSave = IsAffirmative(TerminalUI.Prompt($"Save payment information ({paymentName})? [Y/n]: "));
            TerminalUI.Clear();

            if (shouldSave) {
                Repo.SavePayment(paymentName, p);
                TerminalUI.PrintLine("Payment saved.");
                return p;
            }
            TerminalUI.PrintLine("Payment not saved.");
            return default;
        });
    }

    private string GetPaymentName(string existingName = "") {
        string? paymentName = TerminalUI.PromptForEdit("Payment name: ", existingName);
        if (paymentName is null) {
            TerminalUI.PrintLine("No payment name entered. Try again.");
            return GetPaymentName(existingName);
        }

        if (!paymentName.IsValidName()) {
            TerminalUI.PrintLine("Invalid payment name. Try again.");
            return GetPaymentName(existingName);
        }

        if (Repo.ListPayments().Where(n => n != existingName).Contains(paymentName)) {
            TerminalUI.PrintLine($"Payment '{paymentName}' already exists. Try again.");
            return GetPaymentName(existingName);
        }

        TerminalUI.Clear();
        return paymentName;
    }

    public async Task CreatePaymentsMenu() {
        TerminalUI.PrintLine("--Manage Payments--");

        string[] options = [
            "1. Create new payment info",
            "q. Return"
        ];

        TerminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = TerminalUI.PromptKey("Choose an option: ");
        TerminalUI.Clear();

        switch (choice) {
            case '1': _ = CreatePayment(); await ManagePaymentsMenu(); break;
            case 'Q' or 'q': return;
            default:
                TerminalUI.PrintLine("Not a valid option. Try again.");
                await CreatePaymentsMenu();
                break;
        }
    }

    public async Task ManagePaymentsMenu() {
        if (!Repo.ListPayments().Any()) {
            await CreatePaymentsMenu();
            return;
        }

        TerminalUI.PrintLine("--Manage Payments--");

        string[] options = [
            "1. Create new payment info",
            "2. Edit existing payment info",
            "3. Delete existing payment info",
            "4. Rename existing payment info",
            "q. Return"
        ];
        TerminalUI.PrintLine(string.Join(Environment.NewLine, options));
        var choice = TerminalUI.PromptKey("Choose an option: ");
        TerminalUI.Clear();

        switch (choice) {
            case '1': _ = CreatePayment(); await ManagePaymentsMenu(); break;
            case '2': _ = EditPayment(); await ManagePaymentsMenu(); break;
            case '3': DeletePayment(); await ManagePaymentsMenu(); break;
            case '4': RenamePayment(); await ManagePaymentsMenu(); break;
            case 'Q' or 'q': return;
            default:
                TerminalUI.PrintLine("Not a valid option. Try again.");
                await ManagePaymentsMenu();
                break;
        }
    }

    private void RenamePayment() {
        var paymentName = Chooser.GetUserChoice(
            "Choose a payment to rename: ", Repo.ListPayments(), "payment");
        if (paymentName is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No payment selected.");
            return;
        }

        var newPaymentName = GetPaymentName(paymentName);
        if (paymentName == newPaymentName) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("Payment not renamed.");
            return;
        }
        Repo.RenamePayment(paymentName, newPaymentName);

        TerminalUI.Clear();
        TerminalUI.PrintLine($"Payment renamed to '{newPaymentName}'.");
    }

    public Payment? EditPayment() {
        var paymentName = Chooser.GetUserChoice(
            "Choose a payment to edit: ", Repo.ListPayments(), "payment");
        if (paymentName is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No payment selected.");
            return default;
        }

        var payWithCard = (PaymentInfo.PayWithCard?)Repo.GetPayment(paymentName)?.PaymentInfo
            ?? throw new Exception("Payment not found.");

        TerminalUI.PrintLine($"Editing '{paymentName}' payment information:");
        var cardNumber = TerminalUI.PromptForEdit("Card number: ", payWithCard.CardNumber) ?? "";
        var expiration = TerminalUI.PromptForEdit("Expiration date (MM/YY): ", payWithCard.Expiration) ?? "";
        var cvv = TerminalUI.PromptForEdit("CVV: ", payWithCard.SecurityCode) ?? "";
        var zip = TerminalUI.PromptForEdit("Billing zip code: ", payWithCard.BillingZip) ?? "";

        var payment = new UnvalidatedPayment(
            new PaymentInfo.PayWithCard(
                cardNumber, expiration, cvv, zip)).Parse();

        return payment.Match(es => {
            TerminalUI.PrintLine("Failed to parse payment info:");
            TerminalUI.PrintLine(string.Join(Environment.NewLine, es.Select(e => e.ErrorMessage)));
            var choice = IsAffirmative(TerminalUI.Prompt("Try again? [Y/n]: "));
            TerminalUI.Clear();
            return choice ? EditPayment() : default;
        }, p => {
            TerminalUI.PrintLine("Updated payment:");
            TerminalUI.PrintLine(p.Summarize());
            var shouldSave = IsAffirmative(TerminalUI.Prompt($"Save payment information ({paymentName})? [Y/n]: "));
            TerminalUI.Clear();

            if (shouldSave) {
                Repo.SavePayment(paymentName, p);
                TerminalUI.PrintLine("Payment saved.");
                return p;
            }
            TerminalUI.PrintLine("Payment not saved.");
            return default;
        });
    }

    public void DeletePayment() {
        var paymentName = Chooser.GetUserChoice(
            "Choose a payment to delete: ", Repo.ListPayments(), "payment");
        if (paymentName is null) {
            TerminalUI.Clear();
            TerminalUI.PrintLine("No payment selected.");
            return;
        }

        var payment = Repo.GetPayment(paymentName) ?? throw new Exception("Payment not found.");
        TerminalUI.PrintLine($"Deleting '{paymentName}' payment information:");
        TerminalUI.PrintLine(payment.Summarize());
        var shouldDelete = IsAffirmative(TerminalUI.Prompt($"Delete payment ({paymentName})? [Y/n]: "));
        TerminalUI.Clear();

        if (shouldDelete) {
            Repo.DeletePayment(paymentName);
            TerminalUI.PrintLine("Payment deleted.");
            return;
        }
        TerminalUI.PrintLine("Payment not deleted.");
    }
}
