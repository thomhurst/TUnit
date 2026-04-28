using System.Text;
using System.Text.Json;
using CloudShop.Shared.Events;
using RabbitMQ.Client;

namespace CloudShop.ApiService.Services;

public class OrderEventPublisher(IConnection rabbitConnection, ILogger<OrderEventPublisher> logger)
{
    private const int MaxAttempts = 4;
    private static readonly TimeSpan InitialBackoff = TimeSpan.FromMilliseconds(100);

    public Task PublishOrderCreatedAsync(OrderCreatedEvent orderEvent)
        => PublishAsync("order-events", "order.created", orderEvent);

    public Task PublishPaymentProcessedAsync(OrderPaymentProcessedEvent paymentEvent)
        => PublishAsync("order-events", "order.payment-processed", paymentEvent);

    private async Task PublishAsync<T>(string exchange, string routingKey, T message)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var backoff = InitialBackoff;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await using var channel = await rabbitConnection.CreateChannelAsync();
                await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true);
                await channel.BasicPublishAsync(exchange, routingKey, body);
                return;
            }
            catch (Exception ex)
            {
                if (attempt == MaxAttempts)
                {
                    logger.LogError(ex,
                        "Failed to publish {RoutingKey} after {Max} attempts; giving up",
                        routingKey, MaxAttempts);
                    throw;
                }

                logger.LogWarning(ex,
                    "Failed to publish {RoutingKey} (attempt {Attempt}/{Max}); retrying in {DelayMs}ms",
                    routingKey, attempt, MaxAttempts, backoff.TotalMilliseconds);
                await Task.Delay(backoff);
                backoff *= 2;
            }
        }
    }
}
