using System.Net.Http.Json;
using CloudShop.Shared.Contracts;
using CloudShop.Shared.Models;
using CloudShop.Tests.DataSources;
using CloudShop.Tests.Infrastructure;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace CloudShop.Tests.Tests.Orders;

/// <summary>
/// Tests order placement with various payment and shipping combinations.
///
/// Showcases:
/// - [MatrixDataSource] with [Matrix] for combinatorial testing
/// - 9 test cases from 3 payment methods × 3 shipping options
/// - [MethodDataSource] for valid and invalid order scenarios
/// - Test isolation: each test creates its own product so parallel tests never conflict
/// </summary>
[Category("Integration"), Category("Orders")]
public class OrderPlacementTests
{
    [ClassDataSource<CustomerApiClient>(Shared = SharedType.PerTestSession)]
    public required CustomerApiClient Customer { get; init; }

    [ClassDataSource<AdminApiClient>(Shared = SharedType.PerTestSession)]
    public required AdminApiClient Admin { get; init; }

    [Test]
    [MatrixDataSource]
    public async Task Can_Place_Order_With_Various_Options(
        [Matrix("credit_card", "paypal", "bank_transfer")] string paymentMethod,
        [Matrix("standard", "express", "overnight")] string shippingOption)
    {
        // Each test creates its own product - no shared mutable state
        var product = await Admin.Client.CreateTestProductAsync();

        var request = new CreateOrderRequest(
            [new OrderItemRequest(product.Id, 1)],
            paymentMethod,
            shippingOption);

        var response = await Customer.Client.PostAsJsonAsync("/api/orders", request);

        await Assert.That(response).IsCreated();

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.Status).IsEqualTo(OrderStatus.Pending);
        await Assert.That(order.PaymentMethod).IsEqualTo(paymentMethod);
        await Assert.That(order.ShippingOption).IsEqualTo(shippingOption);
        await Assert.That(order.Items).IsNotNull();
        await Assert.That(order.Items.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    [MethodDataSource(typeof(OrderDataSources), nameof(OrderDataSources.ValidOrders))]
    public async Task Valid_Orders_Are_Accepted(CreateOrderRequest template)
    {
        // Create isolated products - one per unique product ID in the template
        var productMap = new Dictionary<int, int>();
        foreach (var item in template.Items.Where(i => !productMap.ContainsKey(i.ProductId)))
        {
            var product = await Admin.Client.CreateTestProductAsync();
            productMap[item.ProductId] = product.Id;
        }

        // Substitute template product IDs with our isolated products
        var request = template with
        {
            Items = template.Items.Select(item =>
                new OrderItemRequest(productMap[item.ProductId], item.Quantity)).ToList()
        };

        var response = await Customer.Client.PostAsJsonAsync("/api/orders", request);

        await Assert.That(response).IsCreated();

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.TotalAmount).IsGreaterThan(0);
    }

    [Test]
    [MethodDataSource(typeof(OrderDataSources), nameof(OrderDataSources.InvalidOrders))]
    public async Task Invalid_Orders_Are_Rejected(InvalidOrderScenario scenario)
    {
        var response = await Customer.Client.PostAsJsonAsync("/api/orders", scenario.Request);

        await Assert.That(response).IsBadRequest();

        var error = await response.Content.ReadAsStringAsync();
        await Assert.That(error.ToLowerInvariant()).Contains(scenario.ExpectedError.ToLowerInvariant());
    }
}
