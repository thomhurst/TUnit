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
/// Showcases:
/// - Custom ActivitySource spans annotating each step of a checkout pipeline
/// - Direct database (PostgreSQL) and cache (Redis) access alongside HTTP calls
/// - Recording an error on a specific span when the operation fails
/// - How the HTML report surfaces the full trace timeline next to a failing test
/// </summary>
[Category("Integration"), Category("Traces")]
public class CheckoutTraceDemoTests
{
    private static readonly ActivitySource Source = new("CloudShop.Tests.Checkout");

    [ClassDataSource<CustomerApiClient>(Shared = SharedType.PerTestSession)]
    public required CustomerApiClient Customer { get; init; }

    [ClassDataSource<AdminApiClient>(Shared = SharedType.PerTestSession)]
    public required AdminApiClient Admin { get; init; }

    [ClassDataSource<RedisFixture>(Shared = SharedType.PerTestSession)]
    public required RedisFixture Redis { get; init; }

    [ClassDataSource<DatabaseFixture>(Shared = SharedType.PerTestSession)]
    public required DatabaseFixture Database { get; init; }

    [Test]
    [Explicit]
    [DisplayName("Checkout fails when requested quantity exceeds available stock")]
    public async Task Checkout_FailsWhenRequestedQuantityExceedsStock()
    {
        // Step 1: Look up a product and its current stock level directly from PostgreSQL
        var productId = await Database.QuerySingleAsync<int>(
            "SELECT \"Id\" FROM \"Products\" WHERE \"Category\" = 'electronics' AND \"StockQuantity\" > 0 LIMIT 1");

        int availableStock;
        using (var dbSpan = Source.StartActivity("db.read_product_stock"))
        {
            availableStock = await Database.QuerySingleAsync<int>(
                "SELECT \"StockQuantity\" FROM \"Products\" WHERE \"Id\" = @id",
                ("id", productId));

            dbSpan?.SetTag("product.id", productId);
            dbSpan?.SetTag("product.stock_available", availableStock);
        }

        // Step 2: Fetch the product through the API — this populates the Redis cache
        using (var apiSpan = Source.StartActivity("api.get_product"))
        {
            var response = await Customer.Client.GetAsync($"/api/products/{productId}");
            apiSpan?.SetTag("product.id", productId);
            apiSpan?.SetTag("http.response.status_code", (int)response.StatusCode);
        }

        // Step 3: Confirm the product is now cached in Redis
        using (var cacheSpan = Source.StartActivity("cache.check_product"))
        {
            var cached = await Redis.Database.StringGetAsync($"product:{productId}");
            cacheSpan?.SetTag("cache.key", $"product:{productId}");
            cacheSpan?.SetTag("cache.hit", cached.HasValue);
        }

        // Step 4: Try to order 500 more units than are actually in stock — the API rejects this
        var requestedQuantity = availableStock + 500;

        using var orderSpan = Source.StartActivity("api.create_order");
        orderSpan?.SetTag("product.id", productId);
        orderSpan?.SetTag("order.quantity_requested", requestedQuantity);
        orderSpan?.SetTag("order.stock_available", availableStock);
        orderSpan?.SetTag("order.overstock_by", 500);

        var orderResponse = await Customer.Client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest(
                [new OrderItemRequest(productId, requestedQuantity)],
                "credit_card",
                "standard"));

        if (!orderResponse.IsSuccessStatusCode)
        {
            var errorBody = await orderResponse.Content.ReadAsStringAsync();
            orderSpan?.SetStatus(ActivityStatusCode.Error, "Order rejected: insufficient stock");
            orderSpan?.SetTag("error.message", errorBody);
        }

        // This assertion fails — we deliberately ordered more than was available
        await Assert.That(orderResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }
}
