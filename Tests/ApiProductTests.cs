using System.Text.Json;

public class ApiProductTests {
    [Fact]
    public void PizzaToProduct() {
        var pizzas = TestPizza.ValidPizzas().Select(p => p.Validate()).ToList();
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, "ApiProducts.json"));
        var expected = JsonSerializer.Deserialize<List<DominosApi.Product>>(json, PizzaSerializer.Options)!;
        var actual = pizzas.Select((p, i) => p.ToProduct(i+1)).ToList();

        Assert.Equal(expected, actual);
    }
}
