using System.Text.Json;

namespace Hollandsoft.OrderPizza;
public interface IPizzaRepo {
    PersonalInfo? GetPersonalInfo();
    NewOrder? GetDefaultOrder();
    Payment? GetDefaultPayment();
    Pizza? GetPizza(string name);
    Payment? GetPayment(string name);

    void SavePersonalInfo(PersonalInfo personalInfo);
    void SavePayment(Payment payment);
}

public class PizzaRepository : IPizzaRepo {
    public Pizza? GetPizza(string name) =>
        DeserializeFromFile<UnvalidatedPizza>($"{name}.json")?.Validate();

    public Payment? GetPayment(string name) =>
        DeserializeFromFile<UnvalidatedPayment>($"{name}.json")?.Validate();

    static T? DeserializeFromFile<T>(string filename) {
        if (!File.Exists(filename)) return default;

        using var fs = File.OpenRead(filename);
        return JsonSerializer.Deserialize<T>(
            fs, PizzaSerializer.Options);
    }

    public PersonalInfo? GetPersonalInfo() =>
        DeserializeFromFile<UnvalidatedPersonalInfo>("personalInfo.json")?.Validate();

    public NewOrder? GetDefaultOrder() =>
        DeserializeFromFile<UnvalidatedOrder>("defaultOrder.json")?.Validate();

    public Payment? GetDefaultPayment() => GetPayment("defaultPayment");

    static void SerializeToFile<T>(string filename, T obj) {
        using var fs = File.Create(filename);
        JsonSerializer.Serialize(
            fs, obj, PizzaSerializer.Options);
    }

    public void SavePersonalInfo(PersonalInfo personalInfo) =>
        SerializeToFile("personalInfo.json", personalInfo);

    public void SavePayment(Payment payment) {
        if (payment.PaymentInfo is PaymentInfo.PayWithCard) {
            SerializeToFile("defaultPayment.json", payment);
        }
    }
}
