using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

public interface IPizzaApi {
    Task<ApiResult> AddPizzaToCart(Pizza userPizza);
    Task<ApiResult> CheckCartTotal();
    Task<ApiResult> OrderPizza(OrderInfo userOrder, PaymentInfo userPayment);
}

public class DominosApi : IPizzaApi {
    private readonly DominosConfig _config;

    private List<Product> _products = new();
    private string? _orderID = null;

    public DominosApi(DominosConfig config) => _config = config;

    public async Task<ApiResult> AddPizzaToCart(Pizza userPizza) {
        ValidateRequest request = new() {
            Order = new() {
                OrderID = _orderID ?? "",
                Products = _products.Append(userPizza.ToProduct(_products.Count + 1)).ToList(),
                ServiceMethod = "Carryout",
                StoreID = _config.StoreID
            }
        };
        var requestJson = JsonSerializer.Serialize(request, PizzaSerializer.Options);

        using var client = new HttpClient();
        var response = await client.PostAsync(
            "https://order.dominos.com/power/validate-order",
            new StringContent(requestJson, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ValidateResponse>(content)!;

        _orderID = result.Order.OrderID;
        _products = result.Order.Products;

        return new ApiResult(true, $"Pizza added to cart. Product Count: {_products.Count} Order Number: {result.Order.OrderID}");
    }

    public Task<ApiResult> CheckCartTotal() =>
        Task.FromResult(_products.Count == 0
        ? new ApiResult(true, "Cart is empty.")
        : new ApiResult(true, "Cart price is $9.99."));

    public Task<ApiResult> OrderPizza(OrderInfo userOrder, PaymentInfo userPayment) =>
        Task.FromResult(_products.Count == 0
        ? new ApiResult(false, "No pizza!")
        : new ApiResult(true, "Order was placed."));

    public void SetOrderID(string orderID) => _orderID = orderID;

    private class ValidateRequest {
        public Order Order { get; set; } = new();
    }
    private class ValidateResponse {
        public Order Order { get; set; } = new();
    }

    private class Order {
        public string OrderID { get; set; } = "";
        public List<Product> Products { get; set; } = new();
        public string ServiceMethod { get; set; } = "";
        public int StoreID { get; set; } = 0;
    }

    public class Options : Dictionary<string, Dictionary<string, string>?> {
        public Options() : base() { }
        public Options(Dictionary<string, Dictionary<string, string>?> ts) : base(ts) { }

        public override bool Equals(object? obj) {
            if (obj is not Dictionary<string, Dictionary<string, string>> o) return false;
            if (this.Count != o.Count) return false;
            if (this.Keys.Except(o.Keys).Any()) return false;
            return true;
        }

        public override int GetHashCode() => base.GetHashCode();
        public override void GetObjectData(SerializationInfo info, StreamingContext context) => base.GetObjectData(info, context);
        public override void OnDeserialization(object? sender) => base.OnDeserialization(sender);
        public override string? ToString() => base.ToString();
    }

    public record Product(int ID, string Code, int Qty, string? Instructions, Options Options);
}

public static class ApiHelpers {
    public static DominosApi.Product ToProduct(this Pizza pizza, int id) => new(
        ID: id,
        Code: GetCode(pizza.Size, pizza.Crust),
        Qty: pizza.Quantity,
        Instructions: GetInstructions(pizza),
        Options: new(pizza.Toppings
            .Select(FromTopping)
            .Concat(FromSauce(pizza.Sauce))
            .Append(FromCheese(pizza.Cheese))
            .ToDictionary(
                t => t.Item1,
                t => t.Item2)));

    private static string? GetInstructions(Pizza pizza) {
        var items = GetItems(pizza).ToList();
        return items.Count == 0 ? null : string.Join('-', items);
        static IEnumerable<string> GetItems(Pizza pizza) {
            if (pizza.Bake == Bake.WellDone) yield return "WD";
            if (pizza.Crust == Crust.Thin && pizza.Cut == Cut.Pie) yield return "PIECT";
            if (pizza.Crust != Crust.Thin && pizza.Cut == Cut.Square) yield return "SQCT";
            if (pizza.Cut == Cut.Uncut) yield return "UNCT";
        }
    }

    private static string GetCode(Size size, Crust crust) {
        var s = size switch {
            Size.Small => "10",
            Size.Medium => "P12",
            Size.Large => "14",
            Size.XL => "P16",
            _ => throw new UnreachableException($"Unknown size: {size}")
        };
        var c = crust switch {
            Crust.Brooklyn => "IBKZA",
            Crust.HandTossed => "SCREEN",
            Crust.Thin => "THIN",
            Crust.HandmadePan => "IPAZA",
            Crust.GlutenFree => "GLUTENF",
            _ => throw new UnreachableException($"Unknown crust: {crust}")
        };
        return s + c;
    }

    private static (string, Dictionary<string, string>?) FromCheese(Cheese cheese) => ("C",
        cheese.Match<Dictionary<string, string>?>(
            full: x => new() { { "1/1", GetA(x) } },
            sides: (l, r) => new() {
                {GetL(Location.Left), GetA(l)},
                {GetL(Location.Right), GetA(r)}
            },
            none: () => null));

    private static IEnumerable<(string, Dictionary<string, string>?)> FromSauce(Sauce? sauce) {
        if (sauce is not Sauce s) {
            yield return ("X", null);
            yield break;
        }
        var t = s.SauceType switch {
            SauceType.Tomato => "X",
            SauceType.Marinara => "Xm",
            SauceType.HoneyBBQ => "Bq",
            SauceType.GarlicParmesan => "Xw",
            SauceType.Alfredo => "Xf",
            SauceType.Ranch => "Rd",
            _ => throw new UnreachableException($"Unknown sauce type: {s.SauceType}")
        };
        var l = "1/1";
        var a = GetA(s.Amount);
        yield return (t, new() {{ l, a }});
        if (s.SauceType != SauceType.Tomato) yield return ("X", null);
    }

    public static (string, Dictionary<string, string>?) FromTopping(Topping topping) {
        var t = topping.ToppingType switch {
            ToppingType.Ham => "H",
            ToppingType.Beef => "B",
            ToppingType.Salami => "Sa",
            ToppingType.Pepperoni => "P",
            ToppingType.ItalianSausage => "S",
            ToppingType.PremiumChicken => "Du",
            ToppingType.Bacon => "K",
            ToppingType.PhillySteak => "Pm",
            ToppingType.HotBuffaloSauce => "Ht",
            ToppingType.JalapenoPeppers => "J",
            ToppingType.Onions => "O",
            ToppingType.BananaPeppers => "Z",
            ToppingType.DicedTomatoes => "Td",
            ToppingType.BlackOlives => "R",
            ToppingType.Mushrooms => "M",
            ToppingType.Pineapple => "N",
            ToppingType.CheddarCheese => "Cp",
            ToppingType.GreenPeppers => "G",
            ToppingType.Spinach => "Si",
            ToppingType.RoastedRedPeppers => "Rr",
            ToppingType.FetaCheese => "Fe",
            ToppingType.ShreddedParmesanAsiago => "Cs",
            _ => throw new UnreachableException($"Unknown topping type: {topping.ToppingType}")
        };
        var l = GetL(topping.Location);
        var a = GetA(topping.Amount);
        return (t, new() {{ l, a }});
    }

    private static string GetL(Location loc) => loc switch {
        Location.Left => "1/2",
        Location.Right => "2/2",
        _ => "1/1"
    };

    private static string GetA(Amount? amt) => amt switch {
        Amount.Light => "0.5",
        Amount.Normal => "1",
        Amount.Extra => "1.5",
        _ => "0"
    };
}

public class DominosConfig {
    public int StoreID { get; set; }
}

public class DummyPizzaApi2 : IPizzaApi {
    private readonly bool _cartFail;
    private readonly bool _priceFail;
    private readonly bool _orderFail;

    public List<ApiCall2> Calls = new();

    public DummyPizzaApi2(bool cartFail = false, bool priceFail = false, bool orderFail = false) =>
        (_cartFail, _priceFail, _orderFail) = (cartFail, priceFail, orderFail);

    public Task<ApiResult> AddPizzaToCart(Pizza userPizza) {
        ApiResult result = new(!_cartFail,
            _cartFail
            ? "Pizza was not added to cart."
            : "Pizza added to cart.");
        Calls.Add(new(nameof(AddPizzaToCart), userPizza, result));
        return Task.FromResult(result);
    }

    public Task<ApiResult> CheckCartTotal() {
        ApiResult result = new(!_priceFail,
            _priceFail
            ? "Failed to check cart price."
            : $"Cart price is ${Calls.Count(c => c.Method == nameof(AddPizzaToCart))*8.25:F2}.");
        Calls.Add(new(nameof(CheckCartTotal), "", result));
        return Task.FromResult(result);
    }

    public Task<ApiResult> OrderPizza(OrderInfo userOrder, PaymentInfo userPayment) {
        ApiResult result = new(!_orderFail,
            _orderFail
            ? "Failed to place order."
            : "Order was placed.");
        Calls.Add(new(nameof(OrderPizza), (userOrder, userPayment), result));
        return Task.FromResult(result);
    }
}

public record ApiCall2(string Method, object Body, ApiResult Result);

public class ApiResult {
    public bool Success { get; }
    public string Message { get; }

    public ApiResult(bool success, string message) {
        Success = success;
        Message = message;
    }

    public string Summarize() => Message;
}