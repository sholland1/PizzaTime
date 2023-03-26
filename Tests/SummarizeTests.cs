namespace Tests;

public class SummarizeTests {
    [Fact]
    public void SummarizeAllPizzas() {
        var expected = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, "PizzaSummaries.txt"))
            .Split("\n*\n");
        var actual = TestPizza.ValidPizzas().Select(p => p.Summarize());
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(TestPayment.GenerateValidPayments), MemberType = typeof(TestPayment))]
    public void SummarizePaymentWorks(TestPayment.ValidData data) {
        var actual = data.PaymentInfo.Summarize();
        var expected = File.ReadAllText(Path.Combine(TestPayment.DataDirectory, data.SummaryFile));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(TestOrder.GenerateValidOrders), MemberType = typeof(TestOrder))]
    public void SummarizeOrderWorks(TestOrder.ValidData data) {
        var actual = data.OrderInfo.Summarize();
        var expected = File.ReadAllText(Path.Combine(TestOrder.DataDirectory, data.SummaryFile));
        Assert.Equal(expected, actual);
    }
}