namespace Hollandsoft.OrderPizza;
public interface IPizzaRepo {
    Pizza? GetPizza(string name);
    Payment? GetPayment(string name);
    ActualOrder? GetOrder(string name);
    SavedOrder? GetSavedOrder(string name);
    PersonalInfo? GetPersonalInfo();
    ActualOrder? GetDefaultOrder();

    void SavePersonalInfo(PersonalInfo personalInfo);
    void SavePizza(string name, Pizza pizza);
    void SavePayment(string name, Payment payment);
    void SaveOrder(string name, SavedOrder order);

    IEnumerable<string> ListPizzas();
    IEnumerable<string> ListPayments();
    IEnumerable<string> ListOrders();

    void DeletePizza(string name);
    void DeletePayment(string name);
    void DeleteOrder(string name);

    void SetDefaultOrder(string name);

    ActualOrder GetActualFromSavedOrder(SavedOrder order);

    void RenamePizza(string name, string newName);
    void RenamePayment(string name, string newName);
    void RenameOrder(string name, string newName);
}

public class JsonFilePizzaRepository : IPizzaRepo {
    private readonly ISerializer _serializer;
    public JsonFilePizzaRepository(ISerializer serializer) => _serializer = serializer;

    public Pizza? GetPizza(string name) =>
        DeserializeFromFile<UnvalidatedPizza>(name + ".pizza")?.Validate();
    public Payment? GetPayment(string name) =>
        DeserializeFromFile<UnvalidatedPayment>(name + ".payment")?.Validate();
    public ActualOrder? GetOrder(string name) =>
        ToUnvalidatedOrder(GetSavedOrder(name)!).Validate();
    public SavedOrder? GetSavedOrder(string name) =>
        DeserializeFromFile<SavedOrder>(name + ".order");

    private UnvalidatedActualOrder ToUnvalidatedOrder(SavedOrder savedOrder) => new() {
        Coupons = savedOrder.Coupons,
        OrderInfo = savedOrder.OrderInfo.Validate(),
        Payment = savedOrder.PaymentType switch {
            PaymentType.PayAtStore => Payment.PayAtStoreInstance,
            PaymentType.PayWithCard => GetPayment(savedOrder.PaymentInfoName!)!,
            _ => throw new NotImplementedException("Unknown payment type")
        },
        Pizzas = savedOrder.Pizzas
            .Select(p => GetPizza(p.Name)!.WithQuantity(p.Quantity))
            .ToList()
    };

    public PersonalInfo? GetPersonalInfo() =>
        DeserializeFromFile<UnvalidatedPersonalInfo>("personalInfo")?.Validate();
    public ActualOrder? GetDefaultOrder() =>
        File.Exists("default_order")
            ? GetOrder(File.ReadAllText("default_order")) : null;

    void SerializeToFile<T>(string filename, T obj) {
        using var fs = File.Create(filename + ".json");
        _serializer.Serialize(fs, obj);
    }

    T? DeserializeFromFile<T>(string filename) {
        if (!File.Exists(filename + ".json")) return default;

        using var fs = File.OpenRead(filename + ".json");
        return _serializer.Deserialize<T>(fs);
    }

    public void SavePersonalInfo(PersonalInfo personalInfo) =>
        SerializeToFile("personalInfo", personalInfo);
    public void SavePizza(string name, Pizza pizza) =>
        SerializeToFile(name + ".pizza", pizza);
    public void SaveOrder(string name, SavedOrder order) =>
        SerializeToFile(name + ".order", order);

    public void SavePayment(string name, Payment payment) {
        if (payment.PaymentInfo is PaymentInfo.PayWithCard) {
            SerializeToFile(name + ".payment", payment);
        }
    }

    private static IEnumerable<string> ListItems(string extension) =>
        Directory.EnumerateFiles(".", "*." + extension + ".json")
            .Select(f => Path.GetFileNameWithoutExtension(f.Replace("." + extension, "")));

    public IEnumerable<string> ListPizzas() => ListItems("pizza");
    public IEnumerable<string> ListPayments() => ListItems("payment");
    public IEnumerable<string> ListOrders() => ListItems("order");

    public void DeletePizza(string name) => File.Delete(name + ".pizza.json");
    public void DeletePayment(string name) => File.Delete(name + ".payment.json");
    public void DeleteOrder(string name) => File.Delete(name + ".order.json");

    public void SetDefaultOrder(string name) => File.WriteAllText("default_order", name);

    public ActualOrder GetActualFromSavedOrder(SavedOrder order) =>
        ToUnvalidatedOrder(order).Validate();

    public void RenamePizza(string name, string newName) {
        if (!File.Exists(name + ".pizza.json")) {
            throw new ArgumentException($"Pizza '{name}' does not exist");
        }

        File.Move(name + ".pizza.json", newName + ".pizza.json");

        var ordersToUpdate = ListOrders()
            .Select(o => (o, Order: GetSavedOrder(o)))
            .Where(o => o.Order?.Pizzas.Select(p => p.Name).Contains(name) == true);

        foreach (var (orderName, order) in ordersToUpdate) {
            var updatedPizzas = order!.Pizzas
                .Select(p => p.Name == name ? p with { Name = newName } : p)
                .ToList();
            SaveOrder(orderName, order!.WithPizzas(updatedPizzas));
        }
    }

    public void RenamePayment(string name, string newName) {
        if (!File.Exists(name + ".payment.json")) {
            throw new ArgumentException($"Payment info '{name}' does not exist");
        }

        File.Move(name + ".payment.json", newName + ".payment.json");

        var ordersToUpdate = ListOrders()
            .Select(o => (o, Order: GetSavedOrder(o)))
            .Where(o => o.Order?.PaymentInfoName == name);

        foreach (var (orderName, order) in ordersToUpdate) {
            SaveOrder(orderName, order!.WithPayment((order!.PaymentType, newName)));
        }
    }

    public void RenameOrder(string name, string newName) {
        if (!File.Exists(name + ".order.json")) {
            throw new ArgumentException($"Order '{name}' does not exist");
        }

        File.Move(name + ".order.json", newName + ".order.json");

        if (File.Exists("default_order") && File.ReadAllText("default_order") == name) {
            File.WriteAllText("default_order", newName);
        }
    }
}
