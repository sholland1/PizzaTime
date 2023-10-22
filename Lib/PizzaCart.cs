namespace Hollandsoft.OrderPizza;
public interface ICart {
    Task<AddPizzaResult> AddPizza(Pizza userPizza);
    void AddCoupon(Coupon coupon);
    void RemoveCoupon(Coupon coupon);
    Task<SummaryResult> GetSummary();
    Task<CartResult> PlaceOrder(PersonalInfo personalInfo, PaymentInfo userPayment);
}

public record AddPizzaResult(bool Success, string Message, int ProductCount, string? OrderID) : CartResult(Success, Message) {
    public AddPizzaResult(bool success, string message) : this(success, message, default, null) { }
}

public record SummaryResult(bool Success, string Message, string? WaitTime, decimal? TotalPrice) : CartResult(Success, Message) {
    public SummaryResult(bool success, string message) : this(success, message, null, null) { }
}

public record CartResult(bool Success, string Message);

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

    public async Task<AddPizzaResult> AddPizza(Pizza userPizza) {
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

        return new(true, "Pizza was added to cart.") {
            ProductCount = _products.Count,
            OrderID = response.Order.OrderID
        };
    }

    private static DateTime MoveToNext15MinuteInterval(DateTime d) {
        int minutesToAdd = 15 - (d.Minute % 15);
        return d.AddMinutes(minutesToAdd);
    }

    public async Task<SummaryResult> GetSummary() {
        if (_products.Count == 0 || _orderID == null) {
            return new(false, "Cart is empty.");
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
            return new(false, "Order ID mismatch.");
        }
        if (response.Order.Products.Count != _products.Count) {
            return new(false, "Product count mismatch.");
        }
        if (!response.Order.Products.Normalize().SequenceEqual(_products)) {
            return new(false, "Product mismatch.");
        }
        if (!response.Order.Coupons.All(c => c.Status == 0)) {
            return new(false, "Coupon not fulfilled.");
        }

        _currentTotal = response.Order.Amounts.Payment;

        return new(true, "Found order summary.") {
            TotalPrice = _currentTotal,
            WaitTime = $"{response.Order.EstimatedWaitMinutes} minutes"
        };
    }

    public async Task<CartResult> PlaceOrder(PersonalInfo personalInfo, PaymentInfo userPayment) {
        if (_products.Count == 0 || _orderID == null || _currentTotal == 0) {
            return new(false, "Cart is empty.");
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
            ? new(false, string.Join("\n", response.Order.StatusItems))
            : new(true, "Order was placed.");
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

    private static OrderPayment GetPayment(PaymentInfo payment, decimal price) =>
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
