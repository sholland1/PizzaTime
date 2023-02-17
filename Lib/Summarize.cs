using System.Diagnostics;

public static class SummaryUtils {
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
        var disp = c switch {
            Cheese.None => "no",
            Cheese.Full f => $"{f.Amount}",
            Cheese.Sides s => "(" + (s.Left?.ToString() ?? "None")
                + "|" + (s.Right?.ToString() ?? "None") + ")",
            _ => throw new UnreachableException($"Invalid cheese! Value: {c}")
        };
        return disp + " cheese";
    }
}
