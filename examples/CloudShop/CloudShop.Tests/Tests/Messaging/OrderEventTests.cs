using System.Net.Http.Json;
using CloudShop.Shared.Contracts;
using CloudShop.Shared.Events;
using CloudShop.Tests.Infrastructure;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace CloudShop.Tests.Tests.Messaging;

/// <summary>
/// Tests RabbitMQ event publishing and consumption.
///
/// Showcases:
/// - [ClassDataSource] for RabbitMQ fixture (nested: App → RabbitMq)
/// - [Retry] for eventually-consistent scenarios
/// - MessageCollector pattern for awaiting async messages
/// - Direct infrastructure testing (verifying messages on the bus)
/// - Test isolation: each test creates its own products and uses exclusive queues
/// </summary>
[Category("Integration"), Category("Messaging")]
[NotInParallel("MessagingTests")]
public class OrderEventTests
{
    [ClassDataSource<CustomerApiClient>(Shared = SharedType.PerTestSession)]
    public required CustomerApiClient Customer { get; init; }

    [ClassDataSource<AdminApiClient>(Shared = SharedType.PerTestSession)]
    public required AdminApiClient Admin { get; init; }

    [ClassDataSource<RabbitMqFixture>(Shared = SharedType.PerTestSession)]
    public required RabbitMqFixture RabbitMq { get; init; }

    [Test, Retry(2)]
    public async Task Order_Creation_Publishes_Event()
    {
        // Create an isolated product for this test
        var product = await Admin.Client.CreateTestProductAsync();

        // Subscribe to order events before creating the order (exclusive queue = isolated)
        var collector = await RabbitMq.SubscribeAsync<OrderCreatedEvent>(
            "order-events", "order.created");

        // Create an order using our isolated product
        var response = await Customer.Client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest([new OrderItemRequest(product.Id, 1)]));
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();

        // Wait for the event
        var orderEvent = await collector.WaitForFirstAsync(TimeSpan.FromSeconds(10));

        await Assert.That(orderEvent).IsNotNull();
        await Assert.That(orderEvent.OrderId).IsEqualTo(order!.Id);
        await Assert.That(orderEvent.CustomerEmail).IsEqualTo(Customer.Email);
        await Assert.That(orderEvent.TotalAmount).IsGreaterThan(0);
    }

    [Test, Retry(2)]
    public async Task Payment_Processing_Publishes_Event()
    {
        // Create an isolated product for this test
        var product = await Admin.Client.CreateTestProductAsync();

        // Subscribe to payment events (exclusive queue = isolated)
        var collector = await RabbitMq.SubscribeAsync<OrderPaymentProcessedEvent>(
            "order-events", "order.payment-processed");

        // Create and pay an order using our isolated product
        var createResponse = await Customer.Client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest([new OrderItemRequest(product.Id, 1)]));
        var order = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        await Customer.Client.PostAsJsonAsync(
            $"/api/orders/{order!.Id}/pay",
            new ProcessPaymentRequest("credit_card", "tok_test"));

        // Wait for the payment event
        var paymentEvent = await collector.WaitForFirstAsync(TimeSpan.FromSeconds(10));

        await Assert.That(paymentEvent).IsNotNull();
        await Assert.That(paymentEvent.OrderId).IsEqualTo(order.Id);
        await Assert.That(paymentEvent.PaymentMethod).IsEqualTo("credit_card");
    }
}
