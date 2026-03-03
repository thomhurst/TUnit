using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using CloudShop.Shared.Contracts;
using CloudShop.Tests.Infrastructure;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace CloudShop.Tests.Tests.Traces;

/// <summary>
/// Demonstrates TUnit's trace capture in the HTML report.
///
/// The test drives the API using HTTP calls — the application handles its own
/// Redis caching and database queries internally. Custom spans provide semantic
/// context around the steps of the checkout flow so the trace timeline tells
/// a readable story.
///
/// Run explicitly to see the trace timeline in the generated HTML report.
/// </summary>
[Category("Integration"), Category("Traces")]
public class CheckoutTraceDemoTests
{
    private static readonly ActivitySource Source = new("CloudShop.Tests.Checkout");

    [ClassDataSource<CustomerApiClient>(Shared = SharedType.PerTestSession)]
    public required CustomerApiClient Customer { get; init; }

    [ClassDataSource<AdminApiClient>(Shared = SharedType.PerTestSession)]
    public required AdminApiClient Admin { get; init; }

    [Test]
    [Explicit]
    [DisplayName("Checkout fails when requested quantity exceeds available stock")]
    public async Task Checkout_FailsWhenRequestedQuantityExceedsStock()
    {
        // Step 1: Browse the catalogue — the API queries PostgreSQL and returns products
        ProductResponse product;
        using (var browseSpan = Source.StartActivity("browse.list_products"))
        {
            var catalogue = await Customer.Client
                .GetFromJsonAsync<PagedResult<ProductResponse>>("/api/products?category=electronics&pageSize=1");
            product = catalogue!.Items.First();
            browseSpan?.SetTag("product.id", product.Id);
            browseSpan?.SetTag("product.stock_available", product.StockQuantity);
        }

        // Step 2: View the product detail — the API checks Redis, falls back to PostgreSQL on
        // a cold cache, then caches the result for subsequent requests
        using (var detailSpan = Source.StartActivity("browse.get_product_detail"))
        {
            await Customer.Client.GetAsync($"/api/products/{product.Id}");
            detailSpan?.SetTag("product.id", product.Id);
        }

        // Step 3: View it again — this time the API serves it directly from Redis
        using (var cachedSpan = Source.StartActivity("browse.get_product_detail_cached"))
        {
            await Customer.Client.GetAsync($"/api/products/{product.Id}");
            cachedSpan?.SetTag("product.id", product.Id);
            cachedSpan?.SetTag("cache.expected", true);
        }

        // Step 4: Attempt to order more units than are in stock — the API validates against
        // the database and rejects the request
        var requestedQuantity = product.StockQuantity + 500;
        using var orderSpan = Source.StartActivity("checkout.create_order");
        orderSpan?.SetTag("product.id", product.Id);
        orderSpan?.SetTag("order.quantity_requested", requestedQuantity);
        orderSpan?.SetTag("order.stock_available", product.StockQuantity);

        var orderResponse = await Customer.Client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest(
                [new OrderItemRequest(product.Id, requestedQuantity)],
                "credit_card",
                "standard"));

        if (!orderResponse.IsSuccessStatusCode)
        {
            var body = await orderResponse.Content.ReadAsStringAsync();
            orderSpan?.SetStatus(ActivityStatusCode.Error, "Order rejected: insufficient stock");
            orderSpan?.SetTag("error.message", body);
        }

        // This assertion fails — we deliberately requested more than was available
        await Assert.That(orderResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }
}
