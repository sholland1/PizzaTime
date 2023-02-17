using System.Text.Json;
using static BuilderHelpers;

var p = Build.Medium.Pan()
    .SetCheese(None, Extra)
    .SetSauce(Marinara, Extra)
    .AddTopping(Pepperoni)
    .AddTopping(Bacon, Left, Extra)
    .AddTopping(Mushrooms, Right, Light)
    .AddTopping(Spinach)
    .SetDippingSauces(1, 2, 3)
    .SetBake(WellDone)
    .SetCut(Square)
    .Build(2);

var pp = Build.Large.HandTossed()
    .AddTopping(Pepperoni)
    .Build();

var result = p.Parse().Match(
    vp => "Success!",
    es => string.Join('\n', es));
Console.WriteLine("Validation result: ");
Console.WriteLine(result);
Console.WriteLine(p.Summarize());
Console.WriteLine();
Console.WriteLine(pp.Summarize());

var json = JsonSerializer.Serialize(p, PizzaSerializer.Options);
// File.WriteAllText("pizza3.json", json);
var p2 = JsonSerializer.Deserialize<Pizza>(json, PizzaSerializer.Options);
var json2 = JsonSerializer.Serialize(p2, PizzaSerializer.Options);

Console.WriteLine(p);
Console.WriteLine(json);
Console.WriteLine(json == json2);
Console.WriteLine(p == p2);
