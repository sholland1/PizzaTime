using static Hollandsoft.OrderPizza.CartResult;

namespace Hollandsoft.OrderPizza;
public interface ICart {
    Task<CartResult<AddPizzaSuccess>> AddPizza(Pizza userPizza);
    void AddCoupon(Coupon coupon);
    void RemoveCoupon(Coupon coupon);
    Task<CartResult<SummarySuccess>> GetSummary();
    Task<CartResult<string>> PlaceOrder(PersonalInfo personalInfo, Payment userPayment);
}

public record SummarySuccess(decimal TotalPrice, string WaitTime);
public record AddPizzaSuccess(int ProductCount, string? OrderID);

public static class CartResult {
    public static CartResult<T> Success<T>(T value) where T : class => new CartResult<T>.Success(value);
    private static CartResult<T> Failure<T>(string message) where T : class => new CartResult<T>.Failure(message);

    public static CartResult<AddPizzaSuccess> AddPizzaFailure(string message) => Failure<AddPizzaSuccess>(message);
    public static CartResult<SummarySuccess> SummaryFailure(string message) => Failure<SummarySuccess>(message);
    public static CartResult<string> PlaceOrderFailure(string message) => Failure<string>(message);
}

public abstract record CartResult<T> where T : class {
    internal record Failure(string Message) : CartResult<T>;
    internal record Success(T Value) : CartResult<T>;

    public bool IsSuccess => this is Success;
    public bool IsFailure => this is Failure;

    public string? FailureMessage => (this as Failure)?.Message;
    public T? SuccessValue => (this as Success)?.Value;

    public U Match<U>(Func<string, U> failure, Func<T, U> success) =>
        this switch {
            Failure f => failure(f.Message),
            Success s => success(s.Value),
            _ => throw new NotImplementedException()
        };

    public void Match(Action<string> failure, Action<T> success) {
        switch (this) {
            case Failure f:
                failure(f.Message);
                break;
            case Success s:
                success(s.Value);
                break;
            default:
                throw new NotImplementedException();
        }
    }
}


public class DominosCart : ICart {
    private readonly IOrderApi _api;
    private readonly OrderInfo _orderInfo;
    private readonly HashSet<Coupon> _coupons = new();

    protected List<Product> _products = new();
    private string? _orderID = null;
    private decimal _currentTotal = 0;

    public DominosCart(IOrderApi api, OrderInfo orderInfo) => (_api, _orderInfo) = (api, orderInfo);

    public void AddCoupon(Coupon coupon) => _coupons.Add(coupon);
    public void RemoveCoupon(Coupon coupon) => _coupons.Remove(coupon);

    public async Task<CartResult<AddPizzaSuccess>> AddPizza(Pizza userPizza) {
        _currentTotal = 0;

        var timing = _orderInfo.Timing.Match<string?>(
            () => null, d => $"{MoveToNext15MinuteInterval(d):yyyy-MM-dd HH:mm:ss}");
        var address = _orderInfo.ServiceMethod.Match<OrderAddress?>(Convert, _ => null);

        ValidateRequest request = new() {
            Order = new() {
                Address = address,
                OrderID = _orderID ?? "",
                Products = _products.Append(userPizza.ToProduct(_products.Count + 1)).ToList(),
                ServiceMethod = _orderInfo.ServiceMethod.Name,
                StoreID = _orderInfo.StoreId,
                FutureOrderTime = timing
            }
        };
        var response = await _api.ValidateOrder(request);

        _orderID = response.Order.OrderID;
        _products = response.Order.Products.Normalize().ToList();

        return Success(
            new AddPizzaSuccess(_products.Count, response.Order.OrderID));
    }

    private static DateTime MoveToNext15MinuteInterval(DateTime d) {
        int minutesToAdd = 15 - (d.Minute % 15);
        return d.AddMinutes(minutesToAdd);
    }

    public async Task<CartResult<SummarySuccess>> GetSummary() {
        if (_products.Count == 0 || _orderID == null) {
            return SummaryFailure("Cart is empty.");
        }

        var address = _orderInfo.ServiceMethod.Match<OrderAddress?>(Convert, _ => null);

        PriceRequest request = new() {
            Order = new() {
                Address = address,
                OrderID = _orderID,
                Products = _products,
                ServiceMethod = _orderInfo.ServiceMethod.Name,
                StoreID = _orderInfo.StoreId,
                Coupons = _coupons.ToList()
            }
        };
        var response = await _api.PriceOrder(request);

        if (response.Order.OrderID != _orderID) {
            return SummaryFailure("Order ID mismatch.");
        }
        if (response.Order.Products.Count != _products.Count) {
            return SummaryFailure("Product count mismatch.");
        }
        if (!response.Order.Products.Normalize().SequenceEqual(_products)) {
            return SummaryFailure("Product mismatch.");
        }
        if (!response.Order.Coupons.All(c => c.Status == 0)) {
            return SummaryFailure("Coupon not fulfilled.");
        }

        _currentTotal = response.Order.Amounts.Payment;

        return Success(new SummarySuccess(_currentTotal, $"{response.Order.EstimatedWaitMinutes} minutes"));
    }

    public async Task<CartResult<string>> PlaceOrder(PersonalInfo personalInfo, Payment userPayment) {
        if (_products.Count == 0 || _orderID == null || _currentTotal == 0) {
            return PlaceOrderFailure("Cart is empty.");
        }

        var address = _orderInfo.ServiceMethod.Match<OrderAddress?>(Convert, _ => null);

        PlaceRequest request = new() {
            Order = new() {
                Address = address,
                Coupons = _coupons.ToList(),
                Email = personalInfo.Email,
                FirstName = personalInfo.FirstName,
                LastName = personalInfo.LastName,
                Phone = personalInfo.Phone,
                OrderID = _orderID,
                Payments = new() { GetPayment(userPayment, _currentTotal) },
                Products = _products,
                ServiceMethod = GetDetailedServiceMethod(),
                StoreID = _orderInfo.StoreId,
            }
        };

        var response = await _api.PlaceOrder(request);
        return response.Status == -1
            ? PlaceOrderFailure(string.Join("\n", response.Order.StatusItems))
            : Success("Order was placed.");
    }

    private string GetDetailedServiceMethod() =>
        _orderInfo.ServiceMethod.Match(_ => "Delivery", pl => $"{pl}");

    private OrderAddress Convert(Address addr) {
        var addressParts = addr.StreetAddress.Split(' ');
        var streetNum = addressParts[0];
        var streetName = string.Join(' ', addressParts.Skip(1));
        return new() {
            City = addr.City,
            PostalCode = addr.ZipCode,
            Region = addr.State,
            Street = addr.StreetAddress,
            StreetName = streetName,
            StreetNumber = streetNum,
            Type = $"{addr.AddressType}",
            UnitNumber = addr.Apt?.ToString(),
            UnitType = addr.Apt.HasValue ? "APT" : null
        };
    }

    private static OrderPayment GetPayment(Payment payment, decimal price) =>
        payment.Match<OrderPayment>(
            () => throw new NotImplementedException(),
            c => new() {
                Amount = price,
                CardType = c.Type,
                Expiration = c.Expiration,
                Number = c.CardNumber,
                PostalCode = c.BillingZip,
                SecurityCode = c.SecurityCode,
                Type = "CreditCard"
            });
}
