using System.Text.Json;

public interface IPizzaRepo {
    Pizza GetPizza(string name);
    OrderInfo GetOrderInfo(string name);
    PaymentInfo GetPaymentInfo(string name);
}

public class PizzaRepository : IPizzaRepo {
    public Pizza GetPizza(string name) =>
        DeserializeFromFile<UnvalidatedPizza>($"{name}.json").Validate();

    public OrderInfo GetOrderInfo(string name) =>
        DeserializeFromFile<UnvalidatedOrderInfo>($"{name}.json").Validate();

    public PaymentInfo GetPaymentInfo(string name) =>
        DeserializeFromFile<UnvalidatedPaymentInfo>($"{name}.json").Validate();

    static T DeserializeFromFile<T>(string filename) =>
        JsonSerializer.Deserialize<T>(
            File.OpenRead(filename), PizzaSerializer.Options)!;
}