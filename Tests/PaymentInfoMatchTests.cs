using Hollandsoft.OrderPizza;

namespace Tests;
public class PaymentInfoMatchTests {
    [Fact]
    public void PayAtStoreInstance() {
        var paymentInfo = PaymentInfo.PayAtStoreInstance;
        paymentInfo.Match(() => { }, _ => Assert.Fail());
    }

    [Fact]
    public void ValidatedPayWithCard() {
        var paymentInfo = new UnvalidatedPaymentInfo.PayWithCard("1000200030004000", "01/23", "123", "12345").Validate();
        paymentInfo.Match(() => Assert.Fail(), _ => { });
    }

    [Fact]
    public void PayWithCard() {
        var paymentInfo = new UnvalidatedPaymentInfo.PayWithCard("", "", "", "");
        paymentInfo.Match(() => Assert.Fail(), _ => { });
    }
}
