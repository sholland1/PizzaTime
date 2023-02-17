namespace Tests;

public class SummarizeTests {
    [Theory]
    [MemberData(nameof(TestPizza.GenerateValidPizzas), MemberType = typeof(TestPizza))]
    public void SummarizeWorks(TestPizza.ValidData p) {
        var actual = p.Pizza.Summarize();
        var expected = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, p.SummaryFile));
        Assert.Equal(expected, actual);
    }
}