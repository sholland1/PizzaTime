using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Hollandsoft.PizzaTime;

public class DominosStoreApi(ILogger<DominosStoreApi> _log, ISerializer _serializer) : IStoreApi {
    const string _baseUrl = "https://order.dominos.com";
    const string _trackUrl = "https://tracker.dominos.com/tracker-presentation-service";
    private Dictionary<string, Store> _stores = [];
    private Dictionary<string, MenuCoupon> _coupons = [];

    public async Task<List<string>> ListStores(StoreRequest request) {
        var requestUri = "/power/store-locator" + request.UrlParameters;
        _log.LogDebug(requestUri);

        using HttpClient client = new() {
            BaseAddress = new(_baseUrl),
        };
        var response = await client.GetAsync(requestUri);
        var statusCode = response.StatusCode;
        _log.LogDebug("Response: {StatusCode}", statusCode);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        _log.LogTrace(content);
        var storeResponse = _serializer.Deserialize<StoreResponse>(content)!;

        _stores = storeResponse.Stores.ToDictionary(s => s.StoreID);

        return [.. _stores.Keys];
    }

    public Store? GetStore(string storeId) =>
        _stores.TryGetValue(storeId, out var store) ? store : null;

    public async Task<List<string>> ListCoupons(MenuRequest request) {
        var requestUri = $"/power/store/{request.StoreId}/menu?lang=en&structured=true";
        _log.LogDebug(requestUri);

        using HttpClient client = new() {
            BaseAddress = new(_baseUrl),
        };
        var response = await client.GetAsync(requestUri);
        var statusCode = response.StatusCode;
        _log.LogDebug("Response: {StatusCode}", statusCode);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        _log.LogTrace(content);
        var menuResponse = _serializer.Deserialize<JsonObject>(content)!;

        _coupons = menuResponse["Coupons"]!.AsObject()
            .ToDictionary(c => c.Key, c => c.Value.Deserialize<MenuCoupon>()!);

        return [.. _coupons.Keys];
    }

    public MenuCoupon? GetCoupon(string couponId) =>
        _coupons.TryGetValue(couponId, out var coupon) ? coupon : null;

    public async Task<InitialTrackResponse[]> InitiateTrackOrder(InitialTrackRequest request) {
        var requestUri = $"/v2/orders?phonenumber={request.PhoneNumber.Replace("-", "")}";
        return await TrackImpl<InitialTrackResponse[]>(requestUri);
    }

    public async Task<TrackResponse> TrackOrder(TrackRequest request) =>
        await TrackImpl<TrackResponse>(request.Uri);

    private async Task<TResponse> TrackImpl<TResponse>(string requestUri) {
        _log.LogDebug(requestUri);

        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("accept", "application/json");
        client.DefaultRequestHeaders.Add("dpz-language", "en");
        client.DefaultRequestHeaders.Add("dpz-market", "UNITED_STATES");

        var response = await client.GetAsync(_trackUrl + requestUri);
        _log.LogDebug("Response: {StatusCode}", response.StatusCode);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        _log.LogTrace(content);
        return _serializer.Deserialize<TResponse>(content)!;
    }
}

public interface IStoreApi {
    Task<List<string>> ListStores(StoreRequest request);
    Store? GetStore(string storeId);

    Task<List<string>> ListCoupons(MenuRequest request);
    MenuCoupon? GetCoupon(string couponId);

    Task<InitialTrackResponse[]> InitiateTrackOrder(InitialTrackRequest request);
    Task<TrackResponse> TrackOrder(TrackRequest request);
}

public record TrackRequest(string Uri);

public class TrackResponse {
    public DateTime? StartTime { get; init; }
    public DateTime? OvenTime { get; init; }
    public DateTime? RackTime { get; init; }
    public required string OrderStatus { get; init; }
}

// public enum OrderTrackStatus { MakeLine, Oven, Complete}

public class InitialTrackResponse {
    public string? StoreID { get; init; }
    public required string OrderID { get; init; }
    public string? OrderDescription { get; init; }
    public DateTime? OrderTakeCompleteTime { get; init; }
    public required Actions Actions { get; init; }

    public TrackRequest ToTrackRequest() => new(Actions.Track);
}

public class Actions {
    public required string Track { get; init; }
}

public record InitialTrackRequest(string PhoneNumber);

public class MenuCoupon {
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public string Price { get; init; } = "";
    // public DateOnly? EffectiveOn { get; init; }

    public string Summarize() => $"""
        Coupon Code: {Code}
        Name: {Name}
        Description: {Description ?? "None"}
        Price: ${Price}
        """;
}

public record MenuRequest(string StoreId);

public class StoreRequest {
    public required ServiceMethod ServiceMethod { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }

    public string UrlParameters {
        get {
            List<string> ps = [
                $"type={ServiceMethod.Name}",
                $"c={string.Join(" ", Helper())}"
            ];

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
    public required int Status { get; init; }
    public required string Granularity { get; init; }
    public required List<Store> Stores { get; init; }
}

public class Store {
    public required string StoreID { get; init; }
    public string Phone { get; init; } = "";
    public string AddressDescription { get; init; } = "";
    public string HoursDescription { get; init; } = "";
    public bool AllowDeliveryOrders { get; init; }
    public bool AllowCarryoutOrders { get; init; }
    public required ServiceMethodEstimatedWaitMinutes ServiceMethodEstimatedWaitMinutes { get; init; }
    public required StoreCoordinates StoreCoordinates { get; init; }
    public bool IsOpen { get; init; }

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
    public required string StoreLatitude { get; init; }
    public required string StoreLongitude { get; init; }
}

public class ServiceMethodEstimatedWaitMinutes {
    public EstimatedWaitMinutes? Delivery { get; init; }
    public EstimatedWaitMinutes? Carryout { get; init; }

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
    public required int Min { get; init; }
    public required int Max { get; init; }
}
