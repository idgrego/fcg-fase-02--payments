namespace FCG02.PaymentsAPI.Infrastructure.Messaging;

public class RabbitMqSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    // Publicação de resultados de pagamento (Exchange própria da PaymentsAPI)
    public string Exchange { get; set; } = "fcg.payments.events";

    // Consumo de pedidos emitidos pelo catálogo
    public string CatalogExchange { get; set; } = "fcg.catalog.events";
    public string OrderPlacedQueue { get; set; } = "payments.order-placed";
    public string OrderPlacedRoutingKey { get; set; } = "order.placed";
}