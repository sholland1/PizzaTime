using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Hollandsoft.OrderPizza;
public class DominosApi : IOrderApi {
    private readonly ILogger<DominosApi> _log;
    public DominosApi(ILogger<DominosApi> log) => _log = log;

    public async Task<ValidateResponse> ValidateOrder(ValidateRequest request) =>
        await PostAsync<ValidateRequest, ValidateResponse>(
            "/power/validate-order", request);

    public async Task<PriceResponse> PriceOrder(PriceRequest request) =>
        await PostAsync<PriceRequest, PriceResponse>(
            "/power/price-order", request);

    public async Task<PlaceResponse> PlaceOrder(PlaceRequest request) =>
        await PostAsync<PlaceRequest, PlaceResponse>(
            "/power/place-order", request);

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest request) {
        var requestJson = JsonSerializer.Serialize(request);
        _log.LogDebug("{url}\nRequest:\n{requestJson}", url, requestJson);

        using HttpClient client = new() {
            BaseAddress = new("https://order.dominos.com"),
        };
        var response = await client.PostAsync(url,
            new StringContent(requestJson, Encoding.UTF8, "application/json"));
        var statusCode = response.StatusCode;
        _log.LogDebug("Response: {statusCode}", statusCode);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        _log.LogDebug(content);
        return JsonSerializer.Deserialize<TResponse>(content)!;
    }
}

public interface IOrderApi {
    Task<ValidateResponse> ValidateOrder(ValidateRequest request);
    Task<PriceResponse> PriceOrder(PriceRequest request);
    Task<PlaceResponse> PlaceOrder(PlaceRequest request);
}

public class PlaceRequest {
    public required Order2 Order { get; init; }
}

public class Order2 : Order {
    public string Email { get; init; } = "";
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Phone { get; init; } = "";
    public List<OrderPayment> Payments { get; init; } = new();
    public int? Status { get; init; }
    public List<StatusItem> StatusItems { get; init; } = new();
}

public class StatusItem {
    public string Code { get; set; } = "";
    public int PulseCode { get; set; } = 0;
    public string PulseText { get; set; } = "";
    public override string ToString() =>
        $"Code: \"{Code}\", PulseCode: {PulseCode}, PulseText: \"{PulseText}\"";
}

public class PlaceResponse {
    public Order2 Order { get; init; } = new();
    public int? Status { get; init; }
    public List<StatusItem> StatusItems { get; init; } = new();
}

public class OrderPayment {
    public required decimal Amount { get; init; }
    public required string CardType { get; init; }
    public required string Expiration { get; init; }
    public required string Number { get; init; }
    public required string PostalCode { get; init; }
    public required string SecurityCode { get; init; }
    public required string Type { get; init; }
}

public class ValidateRequest {
    public required Order Order { get; init; }
}
public class ValidateResponse {
    public Order Order { get; set; } = new();
}

public class PriceRequest {
    public required Order Order { get; init; }
}
public class PriceResponse {
    public PricedOrder Order { get; set; } = new();
}

public class PricedOrder {
    public string OrderID { get; set; } = "";
    public List<Product> Products { get; set; } = new();
    public Amounts Amounts { get; set; } = new();
    public string EstimatedWaitMinutes { get; set; } = "";
    public List<Coupon> Coupons { get; set; } = new();
}

public class Amounts {
    public decimal Payment { get; set; } = 0;
}

public class Order {
    public string OrderID { get; set; } = "";
    public List<Product> Products { get; set; } = new();
    public string ServiceMethod { get; set; } = "";
    public string StoreID { get; set; } = "0";
    public List<Coupon> Coupons { get; set; } = new();
    public OrderAddress? Address { get; set; }
    public string? FutureOrderTime { get; set; }
}

public class OrderAddress {
    public required string City { get; set; }
    public required string PostalCode { get; set; }
    public required string Region { get; set; }
    public required string Street { get; set; }
    public required string StreetName { get; set; }
    public required string StreetNumber { get; set; }
    public required string Type { get; set; }
    public string? UnitNumber { get; set; }
    public string? UnitType { get; set; }
}

public class Coupon {
    [SetsRequiredMembers]
    public Coupon(string code) => Code = code;
    public required string Code { get; init; }
    public int Status { get; init; } = 0;
    public List<StatusItem> StatusItems { get; init; } = new();
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
