using System.Text.Json;

public interface IPizzaRepo {
    PersonalInfo? GetPersonalInfo();
    NewOrder? GetDefaultOrder();
    PaymentInfo? GetDefaultPaymentInfo();
    Pizza? GetPizza(string name);
    PaymentInfo? GetPaymentInfo(string name);
}

public class PizzaRepository : IPizzaRepo {
    public Pizza? GetPizza(string name) =>
        DeserializeFromFile<UnvalidatedPizza>($"{name}.json")?.Validate();

    public PaymentInfo? GetPaymentInfo(string name) =>
        DeserializeFromFile<UnvalidatedPaymentInfo>($"{name}.json")?.Validate();

    static T? DeserializeFromFile<T>(string filename) =>
        JsonSerializer.Deserialize<T>(
            File.OpenRead(filename), PizzaSerializer.Options);

    public PersonalInfo? GetPersonalInfo() =>
        DeserializeFromFile<UnvalidatedPersonalInfo>("personalInfo.json")?.Validate();

    public NewOrder? GetDefaultOrder() =>
        DeserializeFromFile<UnvalidatedOrder>("defaultOrder.json")?.Validate();

    public PaymentInfo? GetDefaultPaymentInfo() => GetPaymentInfo("defaultPaymentInfo.json");
}
