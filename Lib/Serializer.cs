using System.Text.Json;

namespace Hollandsoft.PizzaTime;

public interface ISerializer {
    string Serialize<T>(T obj, bool writeIndented = true);
    T? Deserialize<T>(string text);

    void Serialize<T>(FileStream fs, T? obj, bool writeIndented = true);
    T Deserialize<T>(FileStream fs);
}

public class MyJsonSerializer(JsonSerializerOptions? _options = null) : ISerializer {
    public static MyJsonSerializer Instance { get; } = new(PizzaSerializer.Options);

    public JsonSerializerOptions Options { get; } = _options ?? new();
    public JsonSerializerOptions CompactOptions { get; } = new(_options ?? new()) { WriteIndented = false };

    public string Serialize<T>(T obj, bool writeIndented = true) =>
        JsonSerializer.Serialize(obj, writeIndented ? Options : CompactOptions);

    public T? Deserialize<T>(string text) =>
        JsonSerializer.Deserialize<T>(text, Options);

    public void Serialize<T>(FileStream fs, T? obj, bool writeIndented = true) =>
        JsonSerializer.Serialize(fs, obj, writeIndented ? Options : CompactOptions);

    public T Deserialize<T>(FileStream fs) =>
        JsonSerializer.Deserialize<T>(fs, Options)!;
}
