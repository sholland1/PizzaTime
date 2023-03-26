public interface IPizzaApi {
    Task<ApiResult> AddPizzaToCart(Pizza userPizza);
    Task<ApiResult> CheckCartTotal();
    Task<ApiResult> OrderPizza(OrderInfo userOrder, PaymentInfo userPayment);
}

public class DominosApi : IPizzaApi {
    private object _config;
    private UnvalidatedPizza? _pizza;

    public async Task<ApiResult> AddPizzaToCart(Pizza userPizza) {

        _pizza = userPizza;
        return new ApiResult(true, "Pizza added to cart.");
    }

    public Task<ApiResult> CheckCartTotal() =>
        Task.FromResult(_pizza is null
        ? new ApiResult(true, "Cart is empty.")
        : new ApiResult(true, "Cart price is $9.99."));

    public Task<ApiResult> OrderPizza(OrderInfo userOrder, PaymentInfo userPayment) =>
        Task.FromResult(_pizza is null
        ? new ApiResult(false, "No pizza!")
        : new ApiResult(true, "Order was placed."));
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