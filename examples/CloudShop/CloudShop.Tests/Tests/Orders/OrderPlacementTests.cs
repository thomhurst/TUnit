using System.Net;
using System.Net.Http.Json;
using CloudShop.Shared.Contracts;
using CloudShop.Shared.Models;
using CloudShop.Tests.Assertions;
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
/// </summary>
[Category("Integration"), Category("Orders")]
public class OrderPlacementTests
{
    [ClassDataSource<CustomerApiClient>(Shared = SharedType.PerTestSession)]
    public required CustomerApiClient Customer { get; init; }

    [Test]
    [MatrixDataSource]
    public async Task Can_Place_Order_With_Various_Options(
        [Matrix("credit_card", "paypal", "bank_transfer")] string paymentMethod,
        [Matrix("standard", "express", "overnight")] string shippingOption)
    {
        var request = new CreateOrderRequest(
            [new OrderItemRequest(1, 1)],
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
    public async Task Valid_Orders_Are_Accepted(CreateOrderRequest request)
    {
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
