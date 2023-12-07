using Hollandsoft.PizzaTime;
using TestData;

namespace Tests;
public class CartProductTests {
    private readonly MyJsonSerializer _serializer = MyJsonSerializer.Instance;

    [Fact]
    public void PizzaToProduct() {
        var pizzas = TestPizza.ValidPizzas().Select(p => p.Validate()).ToList();
        var json = File.ReadAllText(Path.Combine(TestPizza.DataDirectory, "ApiProducts.json"));
        var expected = _serializer.Deserialize<List<Product>>(json)!;
        var actual = pizzas.Select((p, i) => p.ToProduct(i+1)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestNormalizeProducts() {
        var json1 = File.ReadAllText(Path.Combine("../../../Data/", "RequestProducts.json"));
        var json2 = File.ReadAllText(Path.Combine("../../../Data/", "ResponseProducts.json"));
        var result1 = _serializer.Deserialize<List<Product>>(json1)!.Normalize();
        var result2 = _serializer.Deserialize<List<Product>>(json2)!.Normalize();

        Assert.Equal(result1, result2);
    }
}
