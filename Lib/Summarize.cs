using System.Diagnostics;

public static class SummaryUtils {
    public static string Summarize(this OrderInfo o) =>
        string.Join('\n', new[] {
            $"Order for {o.Timing.Match(() => "now", dt => $"later at {dt}")}",
            $"{o.ServiceMethod.Match(
                    address => $"Delivery from Store #{o.StoreId} to\n{address.Summarize()}",
                    loc => $"Carryout at {loc} at Store #{o.StoreId}")}",
        });

    public static string Summarize(this Address a) =>
        string.Join('\n', new[] {
            a.Name ?? "",
            a.StreetAddress,
            a.Apt.HasValue ? $"Apt. {a.Apt}" : "",
            $"{a.City}, {a.State} {a.ZipCode}",
        }.Where(s => s != "")
        .Select(s => $"  {s}"));

    public static string Summarize(this PaymentInfo p) => $"""
        Name: {p.FirstName} {p.LastName}
        Email: {p.Email}
        Phone: {p.Phone}
        {p.Payment.Match(
            () => "Pay at Store",
            (card, exp, code, zip) => $"Pay with card ending in {card % 10000}")}
        """;

    public static string Summarize(this Pizza p) =>
        string.Join('\n', new[] {
            $"{p.Size} {p.Crust} Pizza x{p.Quantity}",
            p.Cheese.IsStandard ? "" : $"  with {p.Cheese.Display()}",
            p.Sauce != null && p.Sauce.Value.IsStandard ? "" : $"  with {p.Sauce.Display()}",
            p.Bake == Bake.Normal ? "" : $"  well done",
            $"  {p.Cut.Display()}",
            p.Oregano ? "  with oregano" : "",
            p.GarlicCrust ? "  with garlic crust" : "",
        }
        .Concat(p.Toppings.Display())
        .Concat(p.DippingSauce.Display())
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

    private static IEnumerable<string> Display(this DippingSauce ds) {
        if (ds.GarlicAmount > 0) yield return $"Garlic sauce x{ds.GarlicAmount}";
        if (ds.RanchAmount > 0) yield return $"Ranch sauce x{ds.RanchAmount}";
        if (ds.MarinaraAmount > 0) yield return $"Marinara sauce x{ds.MarinaraAmount}";
    }

    private static string Display(this Sauce? s) {
        var typeDisp = s == null ? "no" : $"{s.Value.SauceType}";
        var amtDisp = s == null || s.Value.IsStandard ? "" : $"{s.Value.Amount} ";
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
