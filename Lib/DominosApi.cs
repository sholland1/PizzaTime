using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

public class DominosApi : IOrderApi {
    public async Task<ValidateResponse> ValidateOrder(ValidateRequest request) =>
        await PostAsync<ValidateRequest, ValidateResponse>(
            "/power/validate-order", request);

    public async Task<PriceResponse> PriceOrder(PriceRequest request) =>
        await PostAsync<PriceRequest, PriceResponse>(
            "/power/price-order", request);

    // public Task<OrderResponse> PlaceOrder(OrderRequest request) {
    //     throw new NotImplementedException();
    // }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest request) {
        var requestJson = JsonSerializer.Serialize(request);

        using HttpClient client = new() {
            BaseAddress = new("https://order.dominos.com"),
        };
        var response = await client.PostAsync(url,
            new StringContent(requestJson, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(content)!;
    }
}

public interface IOrderApi {
    Task<ValidateResponse> ValidateOrder(ValidateRequest request);
    Task<PriceResponse> PriceOrder(PriceRequest request);
    // Task<OrderResponse> PlaceOrder(OrderRequest request);
}

public class ValidateRequest {
    public Order Order { get; set; } = new();
}
public class ValidateResponse {
    public Order Order { get; set; } = new();
}

public class PriceRequest {
    public Order Order { get; set; } = new();
}
public class PriceResponse {
    public PricedOrder Order { get; set; } = new();
}

public class PricedOrder {
    public string OrderID { get; set; } = "";
    public List<Product> Products { get; set; } = new();

    public Amounts Amounts { get; set; } = new();
    public string EstimatedWaitMinutes { get; set; } = "";
}

public class Amounts {
    public decimal Payment { get; set; } = 0;
}

public class PricedProduct { }

public class Order {
    public string OrderID { get; set; } = "";
    public List<Product> Products { get; set; } = new();
    public string ServiceMethod { get; set; } = "";
    public int StoreID { get; set; } = 0;
}

public class Options : Dictionary<string, Dictionary<string, string>?> {
    public Options() : base() { }
    public Options(Dictionary<string, Dictionary<string, string>?> ts) : base(ts) { }

    public override bool Equals(object? obj) {
        if (obj is not Dictionary<string, Dictionary<string, string>> o) return false;
        if (Count != o.Count) return false;
        if (Keys.Except(o.Keys).Any()) return false;
        return true;
    }

    public override int GetHashCode() => base.GetHashCode();
    public override void GetObjectData(SerializationInfo info, StreamingContext context) => base.GetObjectData(info, context);
    public override void OnDeserialization(object? sender) => base.OnDeserialization(sender);
    public override string? ToString() => base.ToString();
}

public record Product(int ID, string Code, int Qty, string? Instructions, Options? Options);
