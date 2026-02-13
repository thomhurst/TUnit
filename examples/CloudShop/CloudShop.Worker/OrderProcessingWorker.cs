using System.Text;
using System.Text.Json;
using CloudShop.Shared.Events;
using CloudShop.Shared.Models;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CloudShop.Worker;

public class OrderProcessingWorker(
    IServiceProvider serviceProvider,
    IConnection rabbitConnection,
    ILogger<OrderProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = rabbitConnection.CreateModel();

        channel.ExchangeDeclare("order-events", ExchangeType.Topic, durable: true);
        var queueDeclare = channel.QueueDeclare("order-processing", durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(queueDeclare.QueueName, "order-events", "order.payment-processed");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var paymentEvent = JsonSerializer.Deserialize<OrderPaymentProcessedEvent>(json);

                if (paymentEvent is not null)
                {
                    await FulfillOrderAsync(paymentEvent.OrderId);
                    logger.LogInformation("Fulfilled order {OrderId}", paymentEvent.OrderId);
                }

                channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message");
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        channel.BasicConsume(queueDeclare.QueueName, autoAck: false, consumer: consumer);

        // Keep the worker alive
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task FulfillOrderAsync(int orderId)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();

        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null || order.Status != OrderStatus.PaymentProcessed) return;

        // Simulate some processing time
        await Task.Delay(500);

        order.Status = OrderStatus.Fulfilled;
        order.FulfilledAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
