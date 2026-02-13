using System.Text;
using System.Text.Json;
using CloudShop.Shared.Events;
using RabbitMQ.Client;

namespace CloudShop.ApiService.Services;

public class OrderEventPublisher(IConnection rabbitConnection)
{
    public async Task PublishOrderCreatedAsync(OrderCreatedEvent orderEvent)
    {
        await PublishAsync("order-events", "order.created", orderEvent);
    }

    public async Task PublishPaymentProcessedAsync(OrderPaymentProcessedEvent paymentEvent)
    {
        await PublishAsync("order-events", "order.payment-processed", paymentEvent);
    }

    private async Task PublishAsync<T>(string exchange, string routingKey, T message)
    {
        using var channel = rabbitConnection.CreateModel();
        channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        channel.BasicPublish(exchange, routingKey, null, body);
        await Task.CompletedTask;
    }
}
