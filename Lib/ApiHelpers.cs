using System.Diagnostics;

namespace Hollandsoft.PizzaTime;
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

        if (!o.TryGetValue("C", out Dictionary<string, string>? cValue)) {
            o["C"] = val;
        }
        else if (cValue is not null && cValue!.TryGetValue("1/1", out string? c1Value) && c1Value == "0.0") {
            o["C"] = null;
        }

        if (!o.TryGetValue("X", out Dictionary<string, string>? xValue)) {
            o["X"] = val;
        }
        else if (xValue is not null && xValue!.TryGetValue("1/1", out string? x1Value) && x1Value == "0.0") {
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
