using System.Net.Http.Json;
using CloudShop.Shared.Contracts;
using CloudShop.Tests.Infrastructure;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace CloudShop.Tests.Tests.Performance;

/// <summary>
/// Tests for resilience and eventual consistency scenarios.
///
/// Showcases:
/// - [Retry(N)] for tests that may fail due to timing
/// - [Timeout] for tests with async dependencies
/// - Testing eventually-consistent behavior (worker processing)
/// </summary>
[Category("Resilience")]
public class ResilienceTests
{
    [ClassDataSource<CustomerApiClient>(Shared = SharedType.PerTestSession)]
    public required CustomerApiClient Customer { get; init; }

    [Test, Retry(3), Timeout(20_000)]
    public async Task Order_Is_Eventually_Fulfilled_After_Payment(CancellationToken cancellationToken)
    {
        // Create and immediately pay
        var createResponse = await Customer.Client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest([new OrderItemRequest(1, 1)]));
        var order = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        await Customer.Client.PostAsJsonAsync(
            $"/api/orders/{order!.Id}/pay",
            new ProcessPaymentRequest("credit_card", "tok_test"));

        // Poll for fulfillment (worker processes asynchronously)
        var deadline = DateTime.UtcNow.AddSeconds(15);
        OrderResponse? latestOrder = null;

        while (DateTime.UtcNow < deadline)
        {
            latestOrder = await Customer.Client.GetFromJsonAsync<OrderResponse>(
                $"/api/orders/{order.Id}");

            if (latestOrder?.Status == CloudShop.Shared.Models.OrderStatus.Fulfilled)
                break;

            await Task.Delay(500);
        }

        await Assert.That(latestOrder).IsNotNull();
        await Assert.That(latestOrder!.Status).IsEqualTo(CloudShop.Shared.Models.OrderStatus.Fulfilled);
    }

    [Test, Timeout(5_000)]
    public async Task Health_Endpoint_Responds_Quickly(CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await Customer.Client.GetAsync("/health");
        stopwatch.Stop();

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(2000);
    }
}
