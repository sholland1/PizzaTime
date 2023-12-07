using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Hollandsoft.OrderPizza;

//TODO: Use HttpClientFactory
public class DominosOrderApi(ILogger<DominosOrderApi> _log, ISerializer _serializer) : IOrderApi {
    const string _baseUrl = "https://order.dominos.com";

    public async Task<ValidateResponse> ValidateOrder(ValidateRequest request) =>
        await PostAsync<ValidateRequest, ValidateResponse>(
            "/power/validate-order", request);

    public async Task<PriceResponse> PriceOrder(PriceRequest request) =>
        await PostAsync<PriceRequest, PriceResponse>(
            "/power/price-order", request);

    public async Task<PlaceResponse> PlaceOrder(PlaceRequest request) =>
        await PostAsync<PlaceRequest, PlaceResponse>(
            "/power/place-order", request);

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string requestUri, TRequest request) {
        var requestJson = _serializer.Serialize(request);
        _log.LogDebug("{Url}\nRequest:\n{RequestJson}", requestUri, requestJson);

        using HttpClient client = new() {
            BaseAddress = new(_baseUrl),
        };
        var response = await client.PostAsync(requestUri,
            new StringContent(requestJson, Encoding.UTF8, "application/json"));
        var statusCode = response.StatusCode;
        _log.LogDebug("Response: {StatusCode}", statusCode);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        _log.LogTrace(content);
        return _serializer.Deserialize<TResponse>(content)!;
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
    public List<OrderPayment> Payments { get; init; } = [];
    public int? Status { get; init; }
    public List<StatusItem> StatusItems { get; init; } = [];
}

public class StatusItem {
    public string Code { get; set; } = "";
    public int PulseCode { get; set; }
    public string PulseText { get; set; } = "";
    public override string ToString() =>
        $"Code: \"{Code}\", PulseCode: {PulseCode}, PulseText: \"{PulseText}\"";
}

public class PlaceResponse {
    public Order2 Order { get; init; } = new();
    public int? Status { get; init; }
    public List<StatusItem> StatusItems { get; init; } = [];
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
    public List<Product> Products { get; set; } = [];
    public Amounts Amounts { get; set; } = new();
    public string EstimatedWaitMinutes { get; set; } = "";
    public List<Coupon> Coupons { get; set; } = [];
}

public class Amounts {
    public decimal Payment { get; set; } = 0;
}

public class Order {
    public string OrderID { get; set; } = "";
    public List<Product> Products { get; set; } = [];
    public string ServiceMethod { get; set; } = "";
    public string StoreID { get; set; } = "0";
    public List<Coupon> Coupons { get; set; } = [];
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

[method: SetsRequiredMembers]
public class Coupon(string code) {
    public required string Code { get; init; } = code;
    public int Status { get; init; }
    public List<StatusItem> StatusItems { get; init; } = [];
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
    public override void OnDeserialization(object? sender) => base.OnDeserialization(sender);
    public override string? ToString() => base.ToString();
}

public record Product(int ID, string Code, int Qty, string? Instructions, Options? Options);
