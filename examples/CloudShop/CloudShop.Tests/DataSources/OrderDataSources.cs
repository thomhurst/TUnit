using System.Net;
using CloudShop.Shared.Contracts;

namespace CloudShop.Tests.DataSources;

/// <summary>
/// Provides test data for order-related tests using [MethodDataSource].
/// Returns Func&lt;T&gt; for reference types to ensure proper test isolation (TUnit best practice).
/// </summary>
public static class OrderDataSources
{
    /// <summary>
    /// Valid order creation requests with different item combinations.
    /// Usage: [MethodDataSource(typeof(OrderDataSources), nameof(ValidOrders))]
    /// </summary>
    public static IEnumerable<Func<CreateOrderRequest>> ValidOrders()
    {
        // Single item order
        yield return () => new([new(1, 1)], "credit_card", "standard");

        // Multi-item order
        yield return () => new([new(1, 2), new(2, 1)], "paypal", "express");

        // Large quantity order
        yield return () => new([new(3, 5)], "bank_transfer", "overnight");

        // Multiple different items
        yield return () => new([new(1, 1), new(2, 2), new(3, 3)], "credit_card", "standard");
    }

    /// <summary>
    /// Invalid order requests that should be rejected, with expected error messages.
    /// Usage: [MethodDataSource(typeof(OrderDataSources), nameof(InvalidOrders))]
    /// </summary>
    public static IEnumerable<Func<InvalidOrderScenario>> InvalidOrders()
    {
        // Empty order
        yield return () => new(new([], "credit_card", "standard"), "at least one item");

        // Invalid product ID
        yield return () => new(new([new(-1, 1)], "credit_card", "standard"), "not found");

        // Zero quantity
        yield return () => new(new([new(1, 0)], "credit_card", "standard"), "quantity");

        // Negative quantity
        yield return () => new(new([new(1, -5)], "credit_card", "standard"), "quantity");
    }

    /// <summary>
    /// Protected API endpoints with expected access results per role.
    /// Used for authorization matrix testing.
    /// </summary>
    public static IEnumerable<EndpointScenario> ProtectedEndpoints()
    {
        yield return new(HttpMethod.Post, "/api/products", HttpStatusCode.Forbidden);
        yield return new(HttpMethod.Delete, "/api/products/1", HttpStatusCode.Forbidden);
        yield return new(HttpMethod.Put, "/api/products/1", HttpStatusCode.Forbidden);
        yield return new(HttpMethod.Get, "/api/products", HttpStatusCode.OK);
        yield return new(HttpMethod.Get, "/api/orders/mine", HttpStatusCode.OK);
    }
}

/// <summary>
/// Describes an invalid order scenario with the expected error message.
/// Uses a concrete type instead of a tuple for AOT compatibility.
/// </summary>
public record InvalidOrderScenario(CreateOrderRequest Request, string ExpectedError);

/// <summary>
/// Describes an API endpoint with its expected status code for non-admin users.
/// </summary>
public record EndpointScenario(HttpMethod Method, string Path, HttpStatusCode ExpectedForCustomer);
