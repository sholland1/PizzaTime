using System.Text.Json;

namespace Hollandsoft.OrderPizza;
public interface IPizzaRepo {
    PersonalInfo? GetPersonalInfo();
    NewOrder? GetDefaultOrder();
    Payment? GetDefaultPayment();
    Pizza? GetPizza(string name);
    Payment? GetPayment(string name);
}

public class PizzaRepository : IPizzaRepo {
    public Pizza? GetPizza(string name) =>
        DeserializeFromFile<UnvalidatedPizza>($"{name}.json")?.Validate();

    public Payment? GetPayment(string name) =>
        DeserializeFromFile<UnvalidatedPayment>($"{name}.json")?.Validate();

    static T? DeserializeFromFile<T>(string filename) =>
        JsonSerializer.Deserialize<T>(
            File.OpenRead(filename), PizzaSerializer.Options);

    public PersonalInfo? GetPersonalInfo() =>
        DeserializeFromFile<UnvalidatedPersonalInfo>("personalInfo.json")?.Validate();

    public NewOrder? GetDefaultOrder() =>
        DeserializeFromFile<UnvalidatedOrder>("defaultOrder.json")?.Validate();

    public Payment? GetDefaultPayment() => GetPayment("defaultPayment");
}
