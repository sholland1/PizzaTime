using Microsoft.Extensions.Logging;

namespace Hollandsoft.OrderPizza;

public class DominosStoreApi : IStoreApi {
    const string _baseUrl = "https://order.dominos.com";
    private readonly ILogger<DominosStoreApi> _log;
    private readonly ISerializer _serializer;
    private Dictionary<string, Store> _stores = new();

    public DominosStoreApi(ILogger<DominosStoreApi> log, ISerializer serializer) =>
        (_log, _serializer) = (log, serializer);

    public async Task<List<string>> ListStores(StoreRequest request) {
        var requestUri = "/power/store-locator" + request.UrlParameters;
        _log.LogDebug(requestUri);

        using HttpClient client = new() {
            BaseAddress = new(_baseUrl),
        };
        var response = await client.GetAsync(requestUri);
        var statusCode = response.StatusCode;
        _log.LogDebug("Response: {statusCode}", statusCode);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        _log.LogDebug(content);
        var storeResponse = _serializer.Deserialize<StoreResponse>(content)!;

        _stores = storeResponse.Stores.ToDictionary(s => s.StoreID);

        return _stores.Keys.ToList();
    }

    public Store? GetStore(string storeId) =>
        _stores.TryGetValue(storeId, out var store) ? store : null;
}

public interface IStoreApi {
    Task<List<string>> ListStores(StoreRequest request);
    Store? GetStore(string storeId);
}

public class StoreRequest {
    public required ServiceMethod ServiceMethod { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }

    public string UrlParameters {
        get {
            List<string> ps = new() {
                $"type={ServiceMethod.Name}",
                $"c={string.Join(" ", Helper())}"
            };

            return "?" + string.Join("&", ps.Select(p => p.Replace(" ", "%20")));

            IEnumerable<string> Helper() {
                if (City is not null) yield return City;
                if (State is not null) yield return State;
                if (ZipCode is not null) yield return ZipCode;
            }
        }
    }
}

public class StoreResponse {
    public required int Status { get; set; }
    public required string Granularity { get; set; }
    public required List<Store> Stores { get; set; }
}

public class Store {
    public required string StoreID { get; set; }
    public string Phone { get; set; } = "";
    public string AddressDescription { get; set; } = "";
    public string HoursDescription { get; set; } = "";
    public bool AllowDeliveryOrders { get; set; }
    public bool AllowCarryoutOrders { get; set; }
    public required ServiceMethodEstimatedWaitMinutes ServiceMethodEstimatedWaitMinutes { get; set; }
    public required StoreCoordinates StoreCoordinates { get; set; }
    public bool IsOpen { get; set; }

    public string Summarize() => $"""
        StoreID: {StoreID} *{(IsOpen ? "Open" : "Closed")}*
        {AddressDescription.Trim()}

        {Phone}
        Hours:
        {HoursDescription}

        Allows delivery orders: {AllowDeliveryOrders}
        Allows carryout orders: {AllowCarryoutOrders}
        {ServiceMethodEstimatedWaitMinutes}
        Coordinates: ({StoreCoordinates.StoreLatitude}, {StoreCoordinates.StoreLongitude})
        """;
}

public class StoreCoordinates {
    public required string StoreLatitude { get; set; }
    public required string StoreLongitude { get; set; }
}

public class ServiceMethodEstimatedWaitMinutes {
    public EstimatedWaitMinutes? Delivery { get; set; }
    public EstimatedWaitMinutes? Carryout { get; set; }

    public override string ToString() {
        return string.Join(Environment.NewLine, Helper());

        IEnumerable<string> Helper() {
            yield return "Estimated wait:";
            if (Delivery is not null) yield return $"  Delivery: {Delivery.Min}-{Delivery.Max} minutes";
            if (Carryout is not null) yield return $"  Carryout: {Carryout.Min}-{Carryout.Max} minutes";
        }
    }
}

public class EstimatedWaitMinutes {
    public required int Min { get; set; }
    public required int Max { get; set; }
}
