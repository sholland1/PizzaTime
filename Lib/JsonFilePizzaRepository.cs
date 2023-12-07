namespace Hollandsoft.PizzaTime;
public interface IPizzaRepo {
    Pizza? GetPizza(string name);
    Payment? GetPayment(string name);
    ActualOrder? GetOrder(string name);
    SavedOrder? GetSavedOrder(string name);
    PersonalInfo? GetPersonalInfo();
    NamedOrder? GetDefaultOrder();

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

    void AddOrderToHistory(PastOrder pastOrder);
    IEnumerable<OrderInstance> ListPastOrders();
    PastOrder GetPastOrder(OrderInstance orderInstance);
}

public class JsonFilePizzaRepository(ISerializer _serializer, FileSystem _fileSystem) : IPizzaRepo {
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

    public NamedOrder? GetDefaultOrder() {
        if (!_fileSystem.Exists("default_order")) return null;

        var orderName = _fileSystem.ReadAllText("default_order");
        var order = GetOrder(orderName);
        if (order is not null) return new(orderName, order);

        _fileSystem.Delete("default_order");
        return null;
    }

    void SerializeToFile<T>(string filename, T obj) {
        using var fs = _fileSystem.Create(filename + ".json");
        _serializer.Serialize(fs, obj);
    }

    T? DeserializeFromFile<T>(string filename) {
        if (!_fileSystem.Exists(filename + ".json")) return default;

        using var fs = _fileSystem.OpenRead(filename + ".json");
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

    private IEnumerable<string> ListItems(string extension) =>
        _fileSystem.EnumerateFiles(".", "*." + extension + ".json")
            .Select(f => Path.GetFileNameWithoutExtension(f.Replace("." + extension, "")));

    public IEnumerable<string> ListPizzas() => ListItems("pizza");
    public IEnumerable<string> ListPayments() => ListItems("payment");
    public IEnumerable<string> ListOrders() => ListItems("order");

    public void DeletePizza(string name) => _fileSystem.Delete(name + ".pizza.json");
    public void DeletePayment(string name) => _fileSystem.Delete(name + ".payment.json");
    public void DeleteOrder(string name) => _fileSystem.Delete(name + ".order.json");

    public void SetDefaultOrder(string name) => _fileSystem.WriteAllText("default_order", name);

    public ActualOrder GetActualFromSavedOrder(SavedOrder order) =>
        ToUnvalidatedOrder(order).Validate();

    public void RenamePizza(string name, string newName) {
        if (!_fileSystem.Exists(name + ".pizza.json")) {
            throw new ArgumentException($"Pizza '{name}' does not exist");
        }

        _fileSystem.Move(name + ".pizza.json", newName + ".pizza.json");

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
        if (!_fileSystem.Exists(name + ".payment.json")) {
            throw new ArgumentException($"Payment info '{name}' does not exist");
        }

        _fileSystem.Move(name + ".payment.json", newName + ".payment.json");

        var ordersToUpdate = ListOrders()
            .Select(o => (o, Order: GetSavedOrder(o)))
            .Where(o => o.Order?.PaymentInfoName == name);

        foreach (var (orderName, order) in ordersToUpdate) {
            SaveOrder(orderName, order!.WithPayment((order!.PaymentType, newName)));
        }
    }

    public void RenameOrder(string name, string newName) {
        if (!_fileSystem.Exists(name + ".order.json")) {
            throw new ArgumentException($"Order '{name}' does not exist");
        }

        _fileSystem.Move(name + ".order.json", newName + ".order.json");

        if (_fileSystem.Exists("default_order") && _fileSystem.ReadAllText("default_order") == name) {
            _fileSystem.WriteAllText("default_order", newName);
        }
    }

    const string OrderHistoryFilename = "orderHistory.jsonl";
    private readonly Dictionary<OrderInstance, PastOrder> _orderHistory = [];
    public void AddOrderToHistory(PastOrder pastOrder) =>
        _fileSystem.AppendAllLines(OrderHistoryFilename, _serializer.Serialize(pastOrder, false));

    public IEnumerable<OrderInstance> ListPastOrders() {
        _orderHistory.Clear();
        if (!_fileSystem.Exists(OrderHistoryFilename)) yield break;

        foreach (var line in _fileSystem.ReadLines(OrderHistoryFilename)) {
            var pastOrder = _serializer.Deserialize<PastOrder>(line)!;
            var instance = pastOrder.ToOrderInstance();
            _orderHistory.Add(instance, pastOrder);
            yield return instance;
        }
    }

    public PastOrder GetPastOrder(OrderInstance orderInstance) => _orderHistory[orderInstance];
}

public class FileSystem(string _root) {
    public bool Exists(string path) => File.Exists(Path.Combine(_root, path));
    public void WriteAllText(string path, string contents) => File.WriteAllText(Path.Combine(_root, path), contents);
    public void AppendAllLines(string path, params string[] contents) => File.AppendAllLines(Path.Combine(_root, path), contents);
    public string ReadAllText(string path) => File.ReadAllText(Path.Combine(_root, path));
    public IEnumerable<string> ReadLines(string path) => File.ReadLines(Path.Combine(_root, path));
    public IAsyncEnumerable<string> ReadLinesAsync(string path) => File.ReadLinesAsync(Path.Combine(_root, path));
    public void Move(string sourceFileName, string destFileName) => File.Move(
        Path.Combine(_root, sourceFileName),
        Path.Combine(_root, destFileName));
    public void Delete(string path) => File.Delete(Path.Combine(_root, path));
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern) =>
        Directory.EnumerateFiles(Path.Combine(_root, path), searchPattern);
    public FileStream OpenRead(string path) => File.OpenRead(Path.Combine(_root, path));
    public FileStream Create(string path) => File.Create(Path.Combine(_root, path));
}
