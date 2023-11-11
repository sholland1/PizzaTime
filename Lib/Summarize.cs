using System.Diagnostics;

namespace Hollandsoft.OrderPizza;
public static class SummaryUtils {
    public static string Summarize(this UnvalidatedOrderInfo o) =>
        string.Join(Environment.NewLine, new[] {
            $"Order for {o.Timing.Match(() => "now", dt => $"later at {dt}")}",
            $"{o.ServiceMethod.Match(
                    address => $"Delivery from Store #{o.StoreId} to\n{address.Summarize()}",
                    loc => $"Carryout at {loc} at Store #{o.StoreId}")}",
        });

    public static string Summarize(this Address a) =>
        string.Join(Environment.NewLine, new[] {
            a.Name ?? "",
            a.StreetAddress,
            a.Apt.HasValue ? $"Apt. {a.Apt}" : "",
            $"{a.City}, {a.State} {a.ZipCode}",
        }.Where(s => s != "")
        .Select(s => $"  {s}"));

    public static string Summarize(this UnvalidatedPersonalInfo p) => $"""
        Name: {p.FirstName} {p.LastName}
        Email: {p.Email}
        Phone: {p.Phone}
        """;

    public static string Summarize(this UnvalidatedPayment p) => $"""
        {p.Match(
            () => "Pay at Store",
            c => $"Pay with {c.Type} ending in {c.CardNumber[(^4)..]}")}
        """;

    public static string Summarize(this UnvalidatedPizza p) =>
        string.Join(Environment.NewLine, new[] {
            $"{p.Size} {p.Crust} Pizza x{p.Quantity}",
            p.Cheese.IsStandard ? "" : $"  with {p.Cheese.Display()}",
            p.Sauce != null && p.Sauce.Value.IsStandard ? "" : $"  with {p.Sauce.Display()}",
            p.Bake == Bake.Normal ? "" : $"  well done",
            $"  {p.Cut.Display()}",
            p.Oregano ? "  with oregano" : "",
            p.GarlicCrust ? "  with garlic crust" : "",
        }
        .Concat(p.Toppings.Display())
        .Where(s => s != ""));

    private static IEnumerable<string> Display(this Topping t) {
        if (t.Amount != Amount.Normal) {
            yield return $"{t.Amount}";
        }
        yield return $"{t.ToppingType}";
        if (t.Location != Location.All) {
            yield return $"on the {t.Location}";
        }
    }

    private static IEnumerable<string> Display(this IEnumerable<Topping> ts) {
        if (!ts.Any()) {
            yield return "No Toppings";
            yield break;
        }
        yield return "Toppings:";
        foreach (var t in ts) {
            yield return $" {string.Join(' ', t.Display())}";
        }
    }

    private static string Display(this Sauce? s) {
        var typeDisp = s == null ? "no" : $"{s.Value.SauceType}";
        var amtDisp = s == null || s.Value.IsStandard || s.Value.Amount == Amount.Normal
            ? "" : $"{s.Value.Amount} ";
        return amtDisp + typeDisp + " sauce";
    }

    private static string Display(this Cut c) => c switch {
        Cut.Pie => "pie ",
        Cut.Square => "square ",
        Cut.Uncut => "un",
        _ => throw new UnreachableException($"Invalid cut! Value: {c}")
    } + "cut";

    private static string Display(this Cheese c) {
        var disp = c.Match(
            amt => $"{amt}",
            (left, right) => "(" + (left?.ToString() ?? "None")
                + "|" + (right?.ToString() ?? "None") + ")",
            () => "no");
        return disp + " cheese";
    }
}
