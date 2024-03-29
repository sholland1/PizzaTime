using System.Diagnostics;

namespace Hollandsoft.PizzaTime;
public class UnvalidatedPizza {
    public Size Size { get; init; }
    public Crust Crust { get; init; }
    public Cheese Cheese { get; init; } = new Cheese.Full(Amount.Normal);
    public Sauce? Sauce { get; init; }
    public Toppings Toppings { get; init; } = [];
    public Bake Bake { get; init; }
    public Cut Cut { get; init; }
    public bool Oregano { get; init; }
    public bool GarlicCrust { get; init; }
    public int Quantity { get; init; }

    public UnvalidatedPizza() {}

    public UnvalidatedPizza(
        Size size, Crust crust, Cheese cheese, Sauce? sauce,
        Toppings toppings,
        Bake bake, Cut cut, bool oregano, bool garlicCrust, int quantity) {
        Size = size;
        Crust = crust;
        Cheese = cheese;
        Sauce = sauce;
        Toppings = toppings;
        Bake = bake;
        Cut = cut;
        Oregano = oregano;
        GarlicCrust = garlicCrust;
        Quantity = quantity;
    }

    public UnvalidatedPizza(UnvalidatedPizza p) :
        this(p.Size, p.Crust, p.Cheese, p.Sauce,
            p.Toppings, p.Bake, p.Cut,
            p.Oregano, p.GarlicCrust, p.Quantity) {}

    public static bool operator ==(UnvalidatedPizza? a, UnvalidatedPizza? b) => Equals(a, b);
    public static bool operator !=(UnvalidatedPizza? a, UnvalidatedPizza? b) => !Equals(a, b);

    public override bool Equals(object? obj) =>
        obj is UnvalidatedPizza p
        && Size == p.Size
        && Crust == p.Crust
        && Cheese == p.Cheese
        && Sauce == p.Sauce
        && Toppings == p.Toppings
        && Bake == p.Bake
        && Cut == p.Cut
        && Oregano == p.Oregano
        && GarlicCrust == p.GarlicCrust
        && Quantity == p.Quantity;

    public override int GetHashCode() {
        HashCode hash = new();
        hash.Add(Size);
        hash.Add(Crust);
        hash.Add(Cheese);
        hash.Add(Sauce);
        hash.Add(Toppings);
        hash.Add(Bake);
        hash.Add(Cut);
        hash.Add(Oregano);
        hash.Add(GarlicCrust);
        hash.Add(Quantity);
        return hash.ToHashCode();
    }
}

public enum Size { Small, Medium, Large, XL }

public static class SizeHelpers {
    public static string AllowedCrustsAIPrompt =>
        string.Join("\n", Enum.GetValues<Size>().Select(s => $"{s}: {string.Join(' ', s.AllowedCrusts())}"));
    public static string AllowedCrustsUserInstructions =>
        string.Join("\n", Enum.GetValues<Size>().Select(s => $"{s} allowed crusts: {string.Join(", ", s.AllowedCrusts())}"));

    public static Crust[] AllowedCrusts(this Size size) => size switch {
        Size.Small => [Crust.HandTossed, Crust.Thin, Crust.GlutenFree],
        Size.Medium => [Crust.HandTossed, Crust.Thin, Crust.HandmadePan],
        Size.Large => [Crust.HandTossed, Crust.Thin, Crust.Brooklyn],
        Size.XL => [Crust.Brooklyn],
        _ => Array.Empty<Crust>()
    };
}

public enum Crust { Brooklyn, HandTossed, Thin, HandmadePan, GlutenFree }

public abstract record Cheese {
    public T Match<T>(Func<Amount, T> full, Func<Amount?, Amount?, T> sides, Func<T> none) =>
        this switch {
            Full f => full(f.Amount),
            Sides s => sides(s.Left, s.Right),
            None => none(),
            _ => throw new UnreachableException($"Invalid Cheese! {this}")
        };

    public void Match(Action<Amount> full, Action<Amount?, Amount?> sides, Action none) {
        switch (this) {
            case Full f: full(f.Amount); break;
            case Sides s: sides(s.Left, s.Right); break;
            case None: none(); break;
            default: throw new UnreachableException($"Invalid Cheese! {this}");
        }
    }

    public sealed record Full(Amount Amount) : Cheese {
        public override bool IsStandard => Amount == Amount.Normal;
    }

    public sealed record Sides(Amount? Left, Amount? Right) : Cheese;
    public sealed record None : Cheese;

    public virtual bool IsStandard => false;
}

public enum Amount { Light, Normal, Extra }
public enum Location { All, Left, Right }

public record struct Sauce(SauceType SauceType, Amount Amount) {
    public readonly bool IsStandard => SauceType == SauceType.Tomato;
}

public enum SauceType { Tomato, Marinara, HoneyBBQ, GarlicParmesan, Alfredo, Ranch }

public static class SauceTypeHelpers {
    public static SauceType[] AllSauces => Enum.GetValues<SauceType>();
}

public record struct Topping(ToppingType ToppingType, Location Location, Amount Amount);

public class Toppings : List<Topping> {
    public Toppings() : base() { }
    public Toppings(IEnumerable<Topping> ts) : base(ts) { }

    public override bool Equals(object? obj) =>
        obj is List<Topping> hs && this.SequenceEqual(hs);
    public override int GetHashCode() => base.GetHashCode();

    public static bool operator ==(Toppings a, Toppings b) => a.Equals(b);
    public static bool operator !=(Toppings a, Toppings b) => !a.Equals(b);
}

public enum ToppingType {
    Ham, Beef, Salami, Pepperoni, ItalianSausage,
    PremiumChicken, Bacon, PhillySteak, HotBuffaloSauce,
    JalapenoPeppers, Onions, BananaPeppers, DicedTomatoes,
    BlackOlives, Mushrooms, Pineapple, ShreddedProvolone,
    CheddarCheese, GreenPeppers, Spinach, //RoastedRedPeppers,
    FetaCheese, ShreddedParmesanAsiago
}

public static class ToppingTypeHelpers {
    public static ToppingType[] AllToppings => Enum.GetValues<ToppingType>();
}

public enum Bake { Normal, WellDone }
public static class BakeHelpers {
    public static Bake[] AllBakes => Enum.GetValues<Bake>();
}

public enum Cut { Pie, Square, Uncut }

public static class CutHelpers {
    public static Cut[] AllCuts => Enum.GetValues<Cut>();
}
