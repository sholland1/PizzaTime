public class Pizza {
    public Size Size { get; init; }
    public Crust Crust { get; init; }
    public Cheese Cheese { get; init; } = new Cheese.Full(Amount.Normal);
    public Sauce? Sauce { get; init; }
    public Toppings Toppings { get; init; } = new Toppings();
    public DippingSauce DippingSauce { get; init; }
    public Bake Bake { get; init; }
    public Cut Cut { get; init; }
    public bool Oregano { get; init; }
    public bool GarlicCrust { get; init; }
    public int Quantity { get; init; }

    public Pizza() {}

    public Pizza(
        Size size, Crust crust, Cheese cheese, Sauce? sauce,
        Toppings toppings, DippingSauce dippingSauce,
        Bake bake, Cut cut, bool oregano, bool garlicCrust, int quantity) {
        Size = size;
        Crust = crust;
        Cheese = cheese;
        Sauce = sauce;
        Toppings = toppings;
        DippingSauce = dippingSauce;
        Bake = bake;
        Cut = cut;
        Oregano = oregano;
        GarlicCrust = garlicCrust;
        Quantity = quantity;
    }

    public Pizza(Pizza p) {
        Size = p.Size;
        Crust = p.Crust;
        Cheese = p.Cheese;
        Sauce = p.Sauce;
        Toppings = p.Toppings;
        DippingSauce = p.DippingSauce;
        Bake = p.Bake;
        Cut = p.Cut;
        Oregano = p.Oregano;
        GarlicCrust = p.GarlicCrust;
        Quantity = p.Quantity;
    }

    public override bool Equals(object? obj) =>
        obj is Pizza p
        && Size == p.Size
        && Crust == p.Crust
        && Cheese == p.Cheese
        && Sauce == p.Sauce
        && Toppings == p.Toppings
        && DippingSauce == p.DippingSauce
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
        hash.Add(DippingSauce);
        hash.Add(Bake);
        hash.Add(Cut);
        hash.Add(Oregano);
        hash.Add(GarlicCrust);
        hash.Add(Quantity);
        return hash.ToHashCode();
    }
}

public enum Size { Small, Medium, Large, XL }

public enum Crust { Brooklyn, HandTossed, Thin, HandmadePan, GlutenFree }

public abstract record Cheese {
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
    public bool IsStandard => SauceType == SauceType.Tomato && Amount == Amount.Normal;
}

public enum SauceType { Tomato, Marinara, HoneyBBQ, GarlicParmesan, Alfredo, Ranch }

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
    BlackOlives, Mushrooms, Pineapple, CheddarCheese,
    GreenPeppers, Spinach, RoastedRedPeppers,
    FetaCheese, ShreddedParmesanAsiago
}

public record struct DippingSauce(int GarlicAmount, int RanchAmount, int MarinaraAmount);

public enum Bake { Normal, WellDone }
public enum Cut { Pie, Square, Uncut }
