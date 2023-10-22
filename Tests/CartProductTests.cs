using System.Text.Json;
using Hollandsoft.OrderPizza;
using TestData;

namespace Tests;
public class CartProductTests {
    [Fact]
    public void PizzaToProduct() {
        var pizzas = TestPizza.ValidPizzas().Select(p => p.Validate()).ToList();
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, "ApiProducts.json"));
        var expected = JsonSerializer.Deserialize<List<Product>>(json, PizzaSerializer.Options)!;
        var actual = pizzas.Select((p, i) => p.ToProduct(i+1)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestNormalizeProducts() {
        var json1 = File.ReadAllText(Path.Combine("../../../Data/", "RequestProducts.json"));
        var json2 = File.ReadAllText(Path.Combine("../../../Data/", "ResponseProducts.json"));
        var result1 = JsonSerializer.Deserialize<List<Product>>(json1, PizzaSerializer.Options)!.Normalize();
        var result2 = JsonSerializer.Deserialize<List<Product>>(json2, PizzaSerializer.Options)!.Normalize();

        Assert.Equal(result1, result2);
    }
}
