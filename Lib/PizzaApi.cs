public interface IPizzaApi {
    ApiResult AddPizzaToCart(Pizza userPizza);
    ApiResult CheckCartTotal();
    ApiResult OrderPizza(OrderInfo userOrder, PaymentInfo userPayment);
}

public class DominosApi : IPizzaApi {
    private object _config;
    private UnvalidatedPizza? _pizza;

    public DominosApi(object config) => _config = config;

    public ApiResult AddPizzaToCart(Pizza userPizza) {
        _pizza = userPizza;
        return new ApiResult(true, "Pizza added to cart.");
    }

    public ApiResult CheckCartTotal() =>
        _pizza is null
        ? new ApiResult(true, "Cart is empty.")
        : new ApiResult(true, "Cart price is $9.99.");

    public ApiResult OrderPizza(OrderInfo userOrder, PaymentInfo userPayment) =>
        _pizza is null
        ? new ApiResult(false, "No pizza!")
        : new ApiResult(true, "Order was placed.");
}

public class ApiResult {
    public bool Success { get; }
    public string Message { get; }

    public ApiResult(bool success, string message) {
        Success = success;
        Message = message;
    }

    public string Summarize() => Message;
}