using System.Net.Http.Json;
using CloudShop.Shared.Contracts;
using CloudShop.Shared.Models;
using CloudShop.Tests.Infrastructure;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace CloudShop.Tests.Tests.Orders;

/// <summary>
/// Tests the complete order lifecycle using DependsOn for sequential execution.
///
/// Showcases:
/// - [DependsOn] for test dependency chains: Create → Pay → Verify Fulfillment
/// - [NotInParallel] to prevent interference with other order tests
/// - [Timeout] for async worker verification
/// - Multiple [ClassDataSource] properties accessing different fixtures
/// - Database fixture for direct DB verification alongside API checks
/// </summary>
[Category("E2E"), Category("Orders")]
[NotInParallel("OrderWorkflow")]
public class OrderWorkflowTests
{
    [ClassDataSource<CustomerApiClient>(Shared = SharedType.PerTestSession)]
    public required CustomerApiClient Customer { get; init; }

    [ClassDataSource<DatabaseFixture>(Shared = SharedType.PerTestSession)]
    public required DatabaseFixture Database { get; init; }

    private static int _orderId;

    [Test]
    public async Task Step1_Place_Order()
    {
        var response = await Customer.Client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest(
                [new OrderItemRequest(1, 2), new OrderItemRequest(2, 1)],
                "credit_card",
                "express"));

        await Assert.That(response.IsSuccessStatusCode).IsTrue();

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.Status).IsEqualTo(OrderStatus.Pending);
        await Assert.That(order.Items.Count).IsEqualTo(2);

        _orderId = order.Id;
    }

    [Test, DependsOn(nameof(Step1_Place_Order))]
    public async Task Step2_Process_Payment()
    {
        var response = await Customer.Client.PostAsJsonAsync(
            $"/api/orders/{_orderId}/pay",
            new ProcessPaymentRequest("credit_card", "tok_test_123"));

        await Assert.That(response.IsSuccessStatusCode).IsTrue();

        // Verify via API
        var orderResponse = await Customer.Client.GetFromJsonAsync<OrderResponse>(
            $"/api/orders/{_orderId}");
        await Assert.That(orderResponse!.Status).IsEqualTo(OrderStatus.PaymentProcessed);
    }

    [Test, DependsOn(nameof(Step2_Process_Payment)), Timeout(30_000)]
    public async Task Step3_Worker_Fulfills_Order(CancellationToken cancellationToken)
    {
        // The Worker service listens for PaymentProcessed events on RabbitMQ
        // and updates the order status to Fulfilled.
        // We poll the API until it shows Fulfilled or timeout.
        OrderResponse? order = null;
        var deadline = DateTime.UtcNow.AddSeconds(25);

        while (DateTime.UtcNow < deadline)
        {
            order = await Customer.Client.GetFromJsonAsync<OrderResponse>(
                $"/api/orders/{_orderId}");

            if (order?.Status == OrderStatus.Fulfilled)
                break;

            await Task.Delay(500);
        }

        await Assert.That(order).IsNotNull();
        await Assert.That(order!.Status).IsEqualTo(OrderStatus.Fulfilled);
        await Assert.That(order.FulfilledAt).IsNotNull();
    }

    [Test, DependsOn(nameof(Step3_Worker_Fulfills_Order))]
    public async Task Step4_Verify_In_Database()
    {
        // Direct database verification using the DatabaseFixture
        var status = await Database.QuerySingleAsync<int>(
            "SELECT \"Status\" FROM \"Orders\" WHERE \"Id\" = @id",
            ("id", _orderId));

        // OrderStatus.Fulfilled = 2
        await Assert.That(status).IsEqualTo((int)OrderStatus.Fulfilled);
    }
}
