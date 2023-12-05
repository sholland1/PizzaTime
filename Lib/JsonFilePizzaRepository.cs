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

public class JsonFilePizzaRepository(ISerializer Serializer, FileSystem FileSystem) : IPizzaRepo {
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
        FileSystem.Exists("default_order")
            ? GetOrder(FileSystem.ReadAllText("default_order")) : null;

    void SerializeToFile<T>(string filename, T obj) {
        using var fs = FileSystem.Create(filename + ".json");
        Serializer.Serialize(fs, obj);
    }

    T? DeserializeFromFile<T>(string filename) {
        if (!FileSystem.Exists(filename + ".json")) return default;

        using var fs = FileSystem.OpenRead(filename + ".json");
        return Serializer.Deserialize<T>(fs);
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

    private IEnumerable<string> ListItems(string extension) =>
        FileSystem.EnumerateFiles(".", "*." + extension + ".json")
            .Select(f => Path.GetFileNameWithoutExtension(f.Replace("." + extension, "")));

    public IEnumerable<string> ListPizzas() => ListItems("pizza");
    public IEnumerable<string> ListPayments() => ListItems("payment");
    public IEnumerable<string> ListOrders() => ListItems("order");

    public void DeletePizza(string name) => FileSystem.Delete(name + ".pizza.json");
    public void DeletePayment(string name) => FileSystem.Delete(name + ".payment.json");
    public void DeleteOrder(string name) => FileSystem.Delete(name + ".order.json");

    public void SetDefaultOrder(string name) => FileSystem.WriteAllText("default_order", name);

    public ActualOrder GetActualFromSavedOrder(SavedOrder order) =>
        ToUnvalidatedOrder(order).Validate();

    public void RenamePizza(string name, string newName) {
        if (!FileSystem.Exists(name + ".pizza.json")) {
            throw new ArgumentException($"Pizza '{name}' does not exist");
        }

        FileSystem.Move(name + ".pizza.json", newName + ".pizza.json");

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
        if (!FileSystem.Exists(name + ".payment.json")) {
            throw new ArgumentException($"Payment info '{name}' does not exist");
        }

        FileSystem.Move(name + ".payment.json", newName + ".payment.json");

        var ordersToUpdate = ListOrders()
            .Select(o => (o, Order: GetSavedOrder(o)))
            .Where(o => o.Order?.PaymentInfoName == name);

        foreach (var (orderName, order) in ordersToUpdate) {
            SaveOrder(orderName, order!.WithPayment((order!.PaymentType, newName)));
        }
    }

    public void RenameOrder(string name, string newName) {
        if (!FileSystem.Exists(name + ".order.json")) {
            throw new ArgumentException($"Order '{name}' does not exist");
        }

        FileSystem.Move(name + ".order.json", newName + ".order.json");

        if (FileSystem.Exists("default_order") && FileSystem.ReadAllText("default_order") == name) {
            FileSystem.WriteAllText("default_order", newName);
        }
    }
}

public class FileSystem {
    private readonly string _root;

    public FileSystem(string root) => _root = root;

    public bool Exists(string path) => File.Exists(Path.Combine(_root, path));
    public void WriteAllText(string path, string contents) => File.WriteAllText(Path.Combine(_root, path), contents);
    public string ReadAllText(string path) => File.ReadAllText(Path.Combine(_root, path));
    public void Move(string sourceFileName, string destFileName) => File.Move(
        Path.Combine(_root, sourceFileName),
        Path.Combine(_root, destFileName));
    public void Delete(string path) => File.Delete(Path.Combine(_root, path));
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern) =>
        Directory.EnumerateFiles(Path.Combine(_root, path), searchPattern);
    public FileStream OpenRead(string path) => File.OpenRead(Path.Combine(_root, path));
    public FileStream Create(string path) => File.Create(Path.Combine(_root, path));
}
