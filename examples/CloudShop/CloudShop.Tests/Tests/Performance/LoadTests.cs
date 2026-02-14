using System.Net.Http.Json;
using CloudShop.Shared.Contracts;
using CloudShop.Tests.Assertions;
using CloudShop.Tests.Infrastructure;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace CloudShop.Tests.Tests.Performance;

/// <summary>
/// Load and concurrency tests using Repeat and ParallelLimiter.
///
/// Showcases:
/// - [Repeat(N)] to run the same test N times
/// - [ParallelLimiter&lt;T&gt;] to control max concurrent test execution
/// - Custom IParallelLimit implementation
/// - [Category("Performance")] for selective test runs
/// </summary>
[Category("Performance")]
public class LoadTests
{
    [ClassDataSource<CustomerApiClient>(Shared = SharedType.PerTestSession)]
    public required CustomerApiClient Customer { get; init; }

    [Test, Repeat(50), ParallelLimiter<TwentyConcurrentLimit>]
    public async Task Product_Listing_Handles_Concurrent_Load()
    {
        var response = await Customer.Client.GetAsync("/api/products?pageSize=10");

        await Assert.That(response.IsSuccessStatusCode).IsTrue();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductResponse>>();
        await Assert.That(result).IsNotNull();
    }

    [Test, Repeat(20), ParallelLimiter<TenConcurrentLimit>]
    public async Task Product_Search_Handles_Concurrent_Load()
    {
        var categories = new[] { "electronics", "clothing", "books" };
        var category = categories[Random.Shared.Next(categories.Length)];

        var response = await Customer.Client.GetAsync(
            $"/api/products?category={category}&pageSize=5");

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }

    [Test, Repeat(30), ParallelLimiter<TenConcurrentLimit>]
    public async Task Order_History_Handles_Concurrent_Load()
    {
        var response = await Customer.Client.GetAsync("/api/orders/mine?pageSize=5");

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }
}

/// <summary>
/// Custom parallel limit: max 20 concurrent tests.
/// </summary>
public class TwentyConcurrentLimit : IParallelLimit
{
    public int Limit => 20;
}

/// <summary>
/// Custom parallel limit: max 10 concurrent tests.
/// </summary>
public class TenConcurrentLimit : IParallelLimit
{
    public int Limit => 10;
}
