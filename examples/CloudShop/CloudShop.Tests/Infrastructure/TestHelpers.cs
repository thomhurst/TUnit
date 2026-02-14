using System.Net.Http.Json;
using CloudShop.Shared.Contracts;

namespace CloudShop.Tests.Infrastructure;

/// <summary>
/// Helpers for creating isolated test resources.
/// Each test creates its own products/data so tests never share mutable state.
/// This enables full parallelism with zero flakiness.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a unique product via the admin API, isolated to the calling test.
    /// Uses high stock quantity so concurrent orders never exhaust it.
    /// </summary>
    public static async Task<ProductResponse> CreateTestProductAsync(
        this HttpClient adminClient,
        decimal price = 99.99m,
        int stockQuantity = 10000)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var request = new CreateProductRequest(
            $"Test Product {uniqueId}",
            "electronics",
            price,
            "Isolated test product",
            stockQuantity);

        var response = await adminClient.PostAsJsonAsync("/api/products", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }
}
