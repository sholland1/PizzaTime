using Hollandsoft.PizzaTime;

namespace Tests;
public class PaymentInfoMatchTests {
    [Fact]
    public void PayAtStoreInstance() {
        var payment = Payment.PayAtStoreInstance;
        payment.Match(() => { }, _ => Assert.Fail());
    }

    [Fact]
    public void ValidatedPayWithCard() {
        var payment = new UnvalidatedPayment(new PaymentInfo.PayWithCard("1000200030004000", "01/23", "123", "12345")).Validate();
        payment.Match(() => Assert.Fail(), _ => { });
    }

    [Fact]
    public void PayWithCard() {
        var payment = new UnvalidatedPayment(new PaymentInfo.PayWithCard("", "", "", ""));
        payment.Match(() => Assert.Fail(), _ => { });
    }
}
