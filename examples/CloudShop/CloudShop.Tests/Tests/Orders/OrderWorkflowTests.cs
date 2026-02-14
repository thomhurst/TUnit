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
/// - TestContext.StateBag for passing data between dependent tests (no static fields)
/// - [NotInParallel] to prevent interference with other order tests
/// - WaitsFor() polling assertion for eventually-consistent state
/// - Multiple [ClassDataSource] properties accessing different fixtures
/// - Database fixture for direct DB verification alongside API checks
/// - Test isolation: creates its own products so it never conflicts with other tests
/// </summary>
[Category("E2E"), Category("Orders")]
[NotInParallel("OrderWorkflow")]
public class OrderWorkflowTests
{
    [ClassDataSource<CustomerApiClient>(Shared = SharedType.PerTestSession)]
    public required CustomerApiClient Customer { get; init; }

    [ClassDataSource<AdminApiClient>(Shared = SharedType.PerTestSession)]
    public required AdminApiClient Admin { get; init; }

    [ClassDataSource<DatabaseFixture>(Shared = SharedType.PerTestSession)]
    public required DatabaseFixture Database { get; init; }

    [Test]
    public async Task Step1_Place_Order()
    {
        // Create isolated products for this workflow
        var product1 = await Admin.Client.CreateTestProductAsync(price: 79.99m);
        var product2 = await Admin.Client.CreateTestProductAsync(price: 49.99m);

        var response = await Customer.Client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest(
                [new OrderItemRequest(product1.Id, 2), new OrderItemRequest(product2.Id, 1)],
                "credit_card",
                "express"));

        await Assert.That(response.IsSuccessStatusCode).IsTrue();

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.Status).IsEqualTo(OrderStatus.Pending);
        await Assert.That(order.Items.Count).IsEqualTo(2);

        // Store the order ID in the StateBag so dependent tests can access it
        TestContext.Current!.StateBag.Items["OrderId"] = order.Id;
    }

    [Test, DependsOn(nameof(Step1_Place_Order))]
    public async Task Step2_Process_Payment()
    {
        // Retrieve the order ID from the dependency's StateBag
        var orderId = (int)TestContext.Current!.Dependencies
            .GetTests(nameof(Step1_Place_Order))[0].StateBag.Items["OrderId"]!;

        var response = await Customer.Client.PostAsJsonAsync(
            $"/api/orders/{orderId}/pay",
            new ProcessPaymentRequest("credit_card", "tok_test_123"));

        await Assert.That(response.IsSuccessStatusCode).IsTrue();

        // Verify via API
        var orderResponse = await Customer.Client.GetFromJsonAsync<OrderResponse>(
            $"/api/orders/{orderId}");
        await Assert.That(orderResponse!.Status).IsEqualTo(OrderStatus.PaymentProcessed);

        // Pass the order ID forward for downstream tests
        TestContext.Current.StateBag.Items["OrderId"] = orderId;
    }

    [Test, DependsOn(nameof(Step2_Process_Payment))]
    public async Task Step3_Worker_Fulfills_Order()
    {
        var orderId = (int)TestContext.Current!.Dependencies
            .GetTests(nameof(Step2_Process_Payment))[0].StateBag.Items["OrderId"]!;

        // The Worker service listens for PaymentProcessed events on RabbitMQ
        // and updates the order status to Fulfilled.
        // WaitsFor polls repeatedly until the assertion passes or the timeout expires.
        var order = await Assert.That(async () =>
                await Customer.Client.GetFromJsonAsync<OrderResponse>($"/api/orders/{orderId}"))
            .WaitsFor(
                assert => assert.Satisfies(o => o?.Status == OrderStatus.Fulfilled),
                timeout: TimeSpan.FromSeconds(25),
                pollingInterval: TimeSpan.FromMilliseconds(500));

        await Assert.That(order).IsNotNull();
        await Assert.That(order!.FulfilledAt).IsNotNull();

        // Pass the order ID forward for the final verification step
        TestContext.Current.StateBag.Items["OrderId"] = orderId;
    }

    [Test, DependsOn(nameof(Step3_Worker_Fulfills_Order))]
    public async Task Step4_Verify_In_Database()
    {
        var orderId = (int)TestContext.Current!.Dependencies
            .GetTests(nameof(Step3_Worker_Fulfills_Order))[0].StateBag.Items["OrderId"]!;

        // Direct database verification using the DatabaseFixture
        var status = await Database.QuerySingleAsync<int>(
            "SELECT \"Status\" FROM \"Orders\" WHERE \"Id\" = @id",
            ("id", orderId));

        // OrderStatus.Fulfilled = 2
        await Assert.That(status).IsEqualTo((int)OrderStatus.Fulfilled);
    }
}
