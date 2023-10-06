using System.Diagnostics;

public interface ICart {
    Task<CartResult> AddPizza(Pizza userPizza);
    Task<CartResult> GetSummary();
    Task<CartResult> PlaceOrder(PaymentInfo userPayment);
}

public class DominosCart : ICart {
    private readonly IOrderApi _api;
    private readonly OrderInfo _orderInfo;

    protected List<Product> _products = new();
    private string? _orderID = null;

    public DominosCart(IOrderApi api, OrderInfo orderInfo) => (_api, _orderInfo) = (api, orderInfo);

    public async Task<CartResult> AddPizza(Pizza userPizza) {
        ValidateRequest request = new() {
            Order = new() {
                OrderID = _orderID ?? "",
                Products = _products.Append(userPizza.ToProduct(_products.Count + 1)).ToList(),
                ServiceMethod = _orderInfo.ServiceMethod.Match(_ => "", _ => "Carryout"), //TODO: stop hard-coding
                StoreID = _orderInfo.StoreId
            }
        };
        var response = await _api.ValidateOrder(request);

        _orderID = response.Order.OrderID;
        _products = response.Order.Products.Normalize().ToList();

        return new(true, $"  Product Count: {_products.Count}\n  Order Number: {response.Order.OrderID}");
    }

    public async Task<CartResult> GetSummary() {
        if (_products.Count == 0 || _orderID == null) {
            return new(false, "Cart is empty.");
        }

        PriceRequest request = new() {
            Order = new() {
                OrderID = _orderID,
                Products = _products,
                ServiceMethod = _orderInfo.ServiceMethod.Match(_ => "", _ => "Carryout"), //TODO: stop hard-coding
                StoreID = _orderInfo.StoreId,
                Coupons = new() { new() { Code = "9220" } } //TODO: stop hard-coding
            }
        };
        var response = await _api.PriceOrder(request);

        if (response.Order.OrderID != _orderID) {
            return new(false, "Order ID mismatch.");
        }
        if (response.Order.Products.Count != _products.Count) {
            return new(false, "Product count mismatch.");
        }
        if (!response.Order.Products.Normalize().SequenceEqual(_products)) {
            return new(false, "Products don't match.");
        }

        return new(true, $"  Price: ${response.Order.Amounts.Payment}\n  Estimated Wait: {response.Order.EstimatedWaitMinutes} minutes");
    }

    public async Task<CartResult> PlaceOrder(PaymentInfo userPayment) {
        if (_products.Count == 0 || _orderID == null) {
            return new(false, "Cart is empty.");
        }

        PlaceRequest request = new() {
            Order = new() {
                Coupons = new() { new() { Code = "9220" } },//TODO: stop hard-coding
                Email = userPayment.Email,
                FirstName = userPayment.FirstName,
                LastName = userPayment.LastName,
                Phone = userPayment.Phone,
                OrderID = _orderID,
                Payments = new() { GetPayment(userPayment, 8.71M) },//TODO: stop hard-coding
                Products = _products,
                ServiceMethod = _orderInfo.ServiceMethod.Match(_ => "", _ => "DriveThru"), //TODO: stop hard-coding
                StoreID = _orderInfo.StoreId,
            }
        };

        var response = await _api.PlaceOrder(request);
        return response.Status == -1
            ? new(false, string.Join("\n", response.StatusItems))
            : new(true, "Order was placed.");
    }

    private static OrderPayment GetPayment(PaymentInfo payment, decimal price) =>
        payment.Payment.Match<OrderPayment>(
            () => throw new NotImplementedException(),
            c => new() {
                Amount = price,
                CardType = c.Type,
                Expiration = c.Expiration,
                Number = $"{c.CardNumber}",
                PostalCode = c.BillingZip,
                SecurityCode = c.SecurityCode,
                Type = "CreditCard"
            });
}

public static class ApiHelpers {
    public static IEnumerable<Product> Normalize(this IEnumerable<Product> ps) => ps.Select(Normalize);

    public static Product Normalize(this Product p) =>
        p with { Options = p.Options.Normalize() };

    //TODO: don't mutate the dictionaries
    private static Options Normalize(this Options? o) {
        var val = OptVal("1/1", "1");
        if (o is null) {
            return new() {
                ["C"] = val,
                ["X"] = val
            };
        }

        if (!o.ContainsKey("C")) {
            o["C"] = val;
        }
        else if (o["C"] is not null && o["C"]!.ContainsKey("1/1") && o["C"]!["1/1"] == "0.0") {
            o["C"] = null;
        }

        if (!o.ContainsKey("X")) {
            o["X"] = val;
        }
        else if (o["X"] is not null && o["X"]!.ContainsKey("1/1") && o["X"]!["1/1"] == "0.0") {
            o["X"] = null;
        }

        return o;
    }

    public static Product ToProduct(this Pizza pizza, int id) => new(
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
            if (pizza.Crust == Crust.HandTossed && !pizza.GarlicCrust) yield return "NGO";
            if (pizza.Crust == Crust.Thin) {
                if (!pizza.Oregano) yield return "NOOR";
                if (pizza.Cut == Cut.Pie) yield return "PIECT";
            }
            else if (pizza.Cut == Cut.Square) yield return "SQCT";
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
            full: x => OptVal("1/1", GetA(x)),
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
        yield return (t, OptVal(l, a));
        if (s.SauceType != SauceType.Tomato) yield return ("X", null);
    }

    private static Dictionary<string, string> OptVal(string l, string a) => new() { { l, a } };

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
            ToppingType.ShreddedProvoloneCheese => "Cp",
            ToppingType.CheddarCheese => "E",
            ToppingType.GreenPeppers => "G",
            ToppingType.Spinach => "Si",
            // ToppingType.RoastedRedPeppers => "Rr",
            ToppingType.FetaCheese => "Fe",
            ToppingType.ShreddedParmesanAsiago => "Cs",
            _ => throw new UnreachableException($"Unknown topping type: {topping.ToppingType}")
        };
        var l = GetL(topping.Location);
        var a = GetA(topping.Amount);
        return (t, OptVal(l, a));
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

public class DummyPizzaCart2 : ICart {
    private readonly bool _cartFail;
    private readonly bool _priceFail;
    private readonly bool _orderFail;

    public List<MethodCall2> Calls = new();

    public DummyPizzaCart2(bool cartFail = false, bool priceFail = false, bool orderFail = false) =>
        (_cartFail, _priceFail, _orderFail) = (cartFail, priceFail, orderFail);

    public Task<CartResult> AddPizza(Pizza userPizza) {
        CartResult result = new(!_cartFail,
            _cartFail
            ? "Pizza was not added to cart."
            : "Pizza added to cart.");
        Calls.Add(new(nameof(AddPizza), userPizza, result));
        return Task.FromResult(result);
    }

    public Task<CartResult> GetSummary() {
        CartResult result = new(!_priceFail,
            _priceFail
            ? "Failed to check cart price."
            : $"Cart price is ${Calls.Count(c => c.Method == nameof(AddPizza)) * 8.25:F2}.");
        Calls.Add(new(nameof(GetSummary), "", result));
        return Task.FromResult(result);
    }

    public Task<CartResult> PlaceOrder(PaymentInfo userPayment) {
        CartResult result = new(!_orderFail,
            _orderFail
            ? "Failed to place order."
            : "Order was placed.");
        Calls.Add(new(nameof(PlaceOrder), userPayment, result));
        return Task.FromResult(result);
    }
}

public record MethodCall2(string Method, object Body, CartResult Result);

public class CartResult {
    public bool Success { get; }
    public string Message { get; }

    public CartResult(bool success, string message) {
        Success = success;
        Message = message;
    }

    public string Summarize() => Message;
}
