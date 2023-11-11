using System.Text.Json;

namespace Hollandsoft.OrderPizza;

public interface ISerializer {
    string Serialize<T>(T obj);
    T? Deserialize<T>(string text);

    void Serialize<T>(FileStream fs, T? obj);
    T Deserialize<T>(FileStream fs);
}

public class MyJsonSerializer : ISerializer {
    public static MyJsonSerializer Instance { get; } = new(PizzaSerializer.Options);

    public JsonSerializerOptions Options { get; set; }
    public MyJsonSerializer(JsonSerializerOptions? options = null) =>
        Options = options ?? new();

    public string Serialize<T>(T obj) =>
        JsonSerializer.Serialize(obj, Options);

    public T? Deserialize<T>(string text) =>
        JsonSerializer.Deserialize<T>(text, Options);

    public void Serialize<T>(FileStream fs, T? obj) =>
        JsonSerializer.Serialize(fs, obj, Options);

    public T Deserialize<T>(FileStream fs) =>
        JsonSerializer.Deserialize<T>(fs, Options)!;
}
