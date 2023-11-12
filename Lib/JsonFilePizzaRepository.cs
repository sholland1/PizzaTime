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
    IEnumerable<string> ListPayments();

    void DeletePizza(string name);
    void DeletePayment(string paymentName);
}

public class JsonFilePizzaRepository : IPizzaRepo {
    private readonly ISerializer _serializer;
    public JsonFilePizzaRepository(ISerializer serializer) => _serializer = serializer;

    public Pizza? GetPizza(string name) =>
        DeserializeFromFile<UnvalidatedPizza>(name + ".pizza")?.Validate();

    public Payment? GetPayment(string name) =>
        DeserializeFromFile<UnvalidatedPayment>(name + ".payment")?.Validate();

    T? DeserializeFromFile<T>(string filename) {
        if (!File.Exists(filename + ".json")) return default;

        using var fs = File.OpenRead(filename + ".json");
        return _serializer.Deserialize<T>(fs);
    }

    public PersonalInfo? GetPersonalInfo() =>
        DeserializeFromFile<UnvalidatedPersonalInfo>("personalInfo")?.Validate();

    public NewOrder? GetDefaultOrder() =>
        DeserializeFromFile<UnvalidatedOrder>("default.order")?.Validate();

    public Payment? GetDefaultPayment() => GetPayment("default");

    void SerializeToFile<T>(string filename, T obj) {
        using var fs = File.Create(filename + ".json");
        _serializer.Serialize(fs, obj);
    }

    public void SavePersonalInfo(PersonalInfo personalInfo) =>
        SerializeToFile("personalInfo", personalInfo);

    public void SavePayment(string name, Payment payment) {
        if (payment.PaymentInfo is PaymentInfo.PayWithCard) {
            SerializeToFile(name + ".payment", payment);
        }
    }

    public void SavePizza(string name, Pizza pizza) =>
        SerializeToFile(name + ".pizza", pizza);

    public IEnumerable<string> ListPizzas() =>
        Directory.EnumerateFiles(".", "*.pizza.json")
            .Select(f => Path.GetFileNameWithoutExtension(f.Replace(".pizza", "")));

    public IEnumerable<string> ListPayments() =>
        Directory.EnumerateFiles(".", "*.payment.json")
            .Select(f => Path.GetFileNameWithoutExtension(f.Replace(".payment", "")));

    public void DeletePizza(string name) => File.Delete(name + ".pizza.json");
    public void DeletePayment(string paymentName) => File.Delete(paymentName + ".payment.json");
}
