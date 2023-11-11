using Hollandsoft.OrderPizza;

namespace Controllers;
public partial class PizzaController {
    public Payment CreatePaymentInfo() {
        _terminalUI.PrintLine("Please enter your payment information.");
        var cardNumber = _terminalUI.Prompt("Card number: ") ?? "";
        var expiration = _terminalUI.Prompt("Expiration date (MM/YY): ") ?? "";
        var cvv = _terminalUI.Prompt("CVV: ") ?? "";
        var zip = _terminalUI.Prompt("Billing zip code: ") ?? "";

        return ValidatePaymentAndSave(cardNumber, expiration, cvv, zip) ?? CreatePaymentInfo();
    }

    public Payment ManagePayments() {
        var payWithCard = (PaymentInfo.PayWithCard?)_repo.GetDefaultPayment()?.PaymentInfo;
        if (payWithCard is null) {
            return CreatePaymentInfo();
        }

        _terminalUI.PrintLine("Edit your payment information:");
        var cardNumber = _terminalUI.PromptForEdit("Card number: ", payWithCard.CardNumber) ?? "";
        var expiration = _terminalUI.PromptForEdit("Expiration date (MM/YY): ", payWithCard.Expiration) ?? "";
        var cvv = _terminalUI.PromptForEdit("CVV: ", payWithCard.SecurityCode) ?? "";
        var zip = _terminalUI.PromptForEdit("Billing zip code: ", payWithCard.BillingZip) ?? "";

        return ValidatePaymentAndSave(cardNumber, expiration, cvv, zip) ?? ManagePayments();
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
}
