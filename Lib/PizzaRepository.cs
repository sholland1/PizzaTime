using System.Text.Json;

namespace Hollandsoft.OrderPizza;
public interface IPizzaRepo {
    PersonalInfo? GetPersonalInfo();
    NewOrder? GetDefaultOrder();
    Payment? GetDefaultPayment();
    Pizza? GetPizza(string name);
    Payment? GetPayment(string name);

    void SavePersonalInfo(PersonalInfo personalInfo);
    void SavePayment(string name, Payment payment);
    void SavePizza(string name, Pizza pizza);

    IEnumerable<string> ListPizzas();
}

public class PizzaRepository : IPizzaRepo {
    public Pizza? GetPizza(string name) =>
        DeserializeFromFile<UnvalidatedPizza>(name + "Pizza")?.Validate();

    public Payment? GetPayment(string name) =>
        DeserializeFromFile<UnvalidatedPayment>(name)?.Validate();

    static T? DeserializeFromFile<T>(string filename) {
        if (!File.Exists(filename + ".json")) return default;

        using var fs = File.OpenRead(filename + ".json");
        return JsonSerializer.Deserialize<T>(
            fs, PizzaSerializer.Options);
    }

    public PersonalInfo? GetPersonalInfo() =>
        DeserializeFromFile<UnvalidatedPersonalInfo>("personalInfo")?.Validate();

    public NewOrder? GetDefaultOrder() =>
        DeserializeFromFile<UnvalidatedOrder>("defaultOrder")?.Validate();

    public Payment? GetDefaultPayment() => GetPayment("defaultPayment");

    static void SerializeToFile<T>(string filename, T obj) {
        using var fs = File.Create(filename + ".json");
        JsonSerializer.Serialize(
            fs, obj, PizzaSerializer.Options);
    }

    public void SavePersonalInfo(PersonalInfo personalInfo) =>
        SerializeToFile("personalInfo", personalInfo);

    public void SavePayment(string name, Payment payment) {
        if (payment.PaymentInfo is PaymentInfo.PayWithCard) {
            SerializeToFile(name + "Payment", payment);
        }
    }

    public void SavePizza(string name, Pizza pizza) =>
        SerializeToFile(name + "Pizza", pizza);

    public IEnumerable<string> ListPizzas() =>
        Directory.EnumerateFiles(".", "*Pizza.json")
            .Select(f => Path.GetFileNameWithoutExtension(f.Replace("Pizza", "")));
}
