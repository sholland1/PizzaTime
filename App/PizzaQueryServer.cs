using Hollandsoft.OrderPizza;
using System.Net.Sockets;
using System.Text;

namespace Server;
public record PizzaQueryServer(HttpOptions HttpOptions, IPizzaRepo PizzaRepository) {
    public Task StartServer() => Task.Run(() => {
        TcpListener listener = new(HttpOptions.IPAddress, HttpOptions.Port);
        listener.Start();

        while (true) {
            using var client = listener.AcceptTcpClient();
            using var stream = client.GetStream();
            var buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (string.IsNullOrEmpty(message)) break;

            var response = HandleRequest(message);

            byte[] responseBuffer = Encoding.UTF8.GetBytes(response);
            stream.Write(responseBuffer, 0, responseBuffer.Length);
        }
    });

    private string HandleRequest(string request) => request.Split(":") switch {
        [ "pizza", string pizzaName ] => PizzaRepository.GetPizza(pizzaName)?.Summarize() ?? "Pizza not found.",
        [ "payment", string paymentName ] => PizzaRepository.GetPayment(paymentName)?.Summarize() ?? "Payment not found.",
        [ "order", string orderName ] => PizzaRepository.GetOrder(orderName)?.Summarize() ?? "Order not found.",
        _ => "Invalid request"
    };
}
