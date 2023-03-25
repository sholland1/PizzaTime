public class PizzaBuilder<TBuilder> : IPizzaBuilder
    where TBuilder : PizzaBuilder<TBuilder> {
    protected Size _size;
    protected Crust _crust;
    protected Cheese _cheese = new Cheese.Full(Amount.Normal);
    protected Sauce? _sauce = new(SauceType.Tomato, Amount.Normal);
    protected List<Topping> _toppings = new();
    protected Bake _bake;
    protected Cut _cut;
    protected bool _oregano;
    protected bool _garlicCrust;

    protected PizzaBuilder(Size size, Crust crust) {
        _size = size;
        _crust = crust;
    }
    public UnvalidatedPizza Build(int quantity = 1) => new(
        _size, _crust, _cheese, _sauce, new(_toppings),
        _bake, _cut, _oregano, _garlicCrust, quantity);

    public TBuilder AddTopping(ToppingType toppingType, Location location = Location.All, Amount amount = Amount.Normal) {
        _toppings.Add(new(toppingType, location, amount));
        return (TBuilder)this;
    }

    public TBuilder SetNoCheese() {
        _cheese = new Cheese.None();
        return (TBuilder)this;
    }

    public TBuilder SetCheese(Amount amount = Amount.Normal) {
        _cheese = new Cheese.Full(amount);
        return (TBuilder)this;
    }

    public TBuilder SetCheese(Amount? leftAmount, Amount? rightAmount) {
        _cheese =
            !leftAmount.HasValue && !rightAmount.HasValue
                ? new Cheese.None()
            : leftAmount != rightAmount
                ? new Cheese.Sides(leftAmount, rightAmount)
                : new Cheese.Full(leftAmount!.Value);
        return (TBuilder)this;
    }

    public TBuilder SetSauce(SauceType sauceType, Amount amount = Amount.Normal) {
        _sauce = new(sauceType, amount);
        return (TBuilder)this;
    }

    public TBuilder SetCut(Cut cut) {
        _cut = cut;
        return (TBuilder)this;
    }
}

public interface IPizzaBuilder {
    public UnvalidatedPizza Build(int quantity = 1);
}

public static class Build {
    public static class Small {
        public static HandTossedBuilder HandTossed() => new(Size.Small);
        public static ThinBuilder Thin() => new(Size.Small);
        public static BakeableBuilder GlutenFree() => new(Size.Small, Crust.GlutenFree);
    }

    public static class Medium {
        public static HandTossedBuilder HandTossed() => new(Size.Medium);
        public static ThinBuilder Thin() => new(Size.Medium);
        public static BakeableBuilder Pan() => new(Size.Medium, Crust.HandmadePan);
    }

    public static class Large {
        public static HandTossedBuilder HandTossed() => new(Size.Large);
        public static ThinBuilder Thin() => new(Size.Large);
        public static BakeableBuilder Brooklyn() => new(Size.Large, Crust.Brooklyn);
    }

    public static class XL {
        public static BakeableBuilder Brooklyn() => new(Size.XL, Crust.Brooklyn);
    }
}

public class BakeableBuilder : PizzaBuilder<BakeableBuilder> {
    internal BakeableBuilder(Size size, Crust crust) : base(size, crust) { }

    public BakeableBuilder SetBake(Bake bake) {
        _bake = bake;
        return this;
    }
}

public class ThinBuilder : PizzaBuilder<ThinBuilder> {
    internal ThinBuilder(Size size) : base(size, Crust.Thin) => _cut = Cut.Square;

    public ThinBuilder WithOregano() {
        _oregano = true;
        return this;
    }
}

public class HandTossedBuilder : PizzaBuilder<HandTossedBuilder> {
    internal HandTossedBuilder(Size size) : base(size, Crust.HandTossed) { }

    public HandTossedBuilder SetBake(Bake bake) {
        _bake = bake;
        return this;
    }

    public HandTossedBuilder WithGarlicCrust() {
        _garlicCrust = true;
        return this;
    }
}

public static class BuilderHelpers {
    public static readonly ToppingType Ham = ToppingType.Ham;
    public static readonly ToppingType Beef = ToppingType.Beef;
    public static readonly ToppingType Salami = ToppingType.Salami;
    public static readonly ToppingType Pepperoni = ToppingType.Pepperoni;
    public static readonly ToppingType ItalianSausage = ToppingType.ItalianSausage;
    public static readonly ToppingType PremiumChicken = ToppingType.PremiumChicken;
    public static readonly ToppingType Bacon = ToppingType.Bacon;
    public static readonly ToppingType PhillySteak = ToppingType.PhillySteak;
    public static readonly ToppingType HotBuffaloSauce = ToppingType.HotBuffaloSauce;
    public static readonly ToppingType JalapenoPeppers = ToppingType.JalapenoPeppers;
    public static readonly ToppingType Onions = ToppingType.Onions;
    public static readonly ToppingType BananaPeppers = ToppingType.BananaPeppers;
    public static readonly ToppingType DicedTomatoes = ToppingType.DicedTomatoes;
    public static readonly ToppingType BlackOlives = ToppingType.BlackOlives;
    public static readonly ToppingType Mushrooms = ToppingType.Mushrooms;
    public static readonly ToppingType Pineapple = ToppingType.Pineapple;
    public static readonly ToppingType CheddarCheese = ToppingType.CheddarCheese;
    public static readonly ToppingType GreenPeppers = ToppingType.GreenPeppers;
    public static readonly ToppingType Spinach = ToppingType.Spinach;
    public static readonly ToppingType RoastedRedPeppers = ToppingType.RoastedRedPeppers;
    public static readonly ToppingType FetaCheese = ToppingType.FetaCheese;
    public static readonly ToppingType ShreddedParmesanAsiago = ToppingType.ShreddedParmesanAsiago;

    public static readonly SauceType Tomato = SauceType.Tomato;
    public static readonly SauceType Marinara = SauceType.Marinara;
    public static readonly SauceType HoneyBBQ = SauceType.HoneyBBQ;
    public static readonly SauceType GarlicParmesan = SauceType.GarlicParmesan;
    public static readonly SauceType Alfredo = SauceType.Alfredo;
    public static readonly SauceType Ranch = SauceType.Ranch;

    public static readonly Amount Light = Amount.Light;
    public static readonly Amount Normal = Amount.Normal;
    public static readonly Amount Extra = Amount.Extra;
    public static readonly Amount? None = null;

    public static readonly Location All = Location.All;
    public static readonly Location Left = Location.Left;
    public static readonly Location Right = Location.Right;

    public static readonly Bake NormalBake = Bake.Normal;
    public static readonly Bake WellDone = Bake.WellDone;

    public static readonly Cut Pie = Cut.Pie;
    public static readonly Cut Square = Cut.Square;
    public static readonly Cut Uncut = Cut.Uncut;
}
