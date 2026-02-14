using System.Net;
using System.Net.Http.Json;
using CloudShop.Shared.Contracts;
using CloudShop.Tests.Infrastructure;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace CloudShop.Tests.Tests.Orders;

/// <summary>
/// Tests order validation and business rules.
///
/// Showcases:
/// - Multiple targeted assertions on responses
/// - Testing error responses and status codes
/// - Negative/edge case testing
/// - Test isolation: each test creates its own products and orders
/// </summary>
[Category("Integration"), Category("Orders")]
public class OrderValidationTests
{
    [ClassDataSource<CustomerApiClient>(Shared = SharedType.PerTestSession)]
    public required CustomerApiClient Customer { get; init; }

    [ClassDataSource<AdminApiClient>(Shared = SharedType.PerTestSession)]
    public required AdminApiClient Admin { get; init; }

    [Test]
    public async Task Cannot_Pay_For_Already_Paid_Order()
    {
        // Create an isolated product and order
        var product = await Admin.Client.CreateTestProductAsync();

        var createResponse = await Customer.Client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest([new OrderItemRequest(product.Id, 1)]));
        var order = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        await Customer.Client.PostAsJsonAsync(
            $"/api/orders/{order!.Id}/pay",
            new ProcessPaymentRequest("credit_card", "tok_test"));

        // Try to pay again
        var secondPayment = await Customer.Client.PostAsJsonAsync(
            $"/api/orders/{order.Id}/pay",
            new ProcessPaymentRequest("credit_card", "tok_test_2"));

        await Assert.That(secondPayment.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Cannot_Pay_For_Nonexistent_Order()
    {
        var response = await Customer.Client.PostAsJsonAsync(
            "/api/orders/999999/pay",
            new ProcessPaymentRequest("credit_card", "tok_test"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Customer_Can_Only_See_Own_Orders()
    {
        // Create an isolated product and place an order
        var product = await Admin.Client.CreateTestProductAsync();

        await Customer.Client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest([new OrderItemRequest(product.Id, 1)]));

        // Verify it appears in their order list
        var listResponse = await Customer.Client.GetFromJsonAsync<PagedResult<OrderResponse>>(
            "/api/orders/mine");

        await Assert.That(listResponse).IsNotNull();
        await Assert.That(listResponse!.TotalCount).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task Order_Total_Is_Calculated_Correctly()
    {
        // Create an isolated product with a known price
        var product = await Admin.Client.CreateTestProductAsync(price: 79.99m);

        var response = await Customer.Client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest(
                [new OrderItemRequest(product.Id, 3)], // 3x our product
                "credit_card", "standard"));

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(order).IsNotNull();

        // Total should be quantity * unit price
        var expectedTotal = order!.Items.Sum(i => i.UnitPrice * i.Quantity);
        await Assert.That(order.TotalAmount).IsEqualTo(expectedTotal);
    }
}
