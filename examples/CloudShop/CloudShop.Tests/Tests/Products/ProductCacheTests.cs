using System.Net.Http.Json;
using CloudShop.Shared.Contracts;
using CloudShop.Tests.Infrastructure;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace CloudShop.Tests.Tests.Products;

/// <summary>
/// Tests Redis caching behavior for products.
///
/// Showcases:
/// - Multiple [ClassDataSource] properties on one class (Admin, Customer, Redis)
/// - Three levels of nested fixtures (App → Admin/Customer/Redis)
/// - Direct infrastructure verification (checking Redis directly)
/// - [NotInParallel] to avoid cache interference between tests
/// </summary>
[Category("Integration"), Category("Cache")]
[NotInParallel("CacheTests")]
public class ProductCacheTests
{
    [ClassDataSource<AdminApiClient>(Shared = SharedType.PerTestSession)]
    public required AdminApiClient Admin { get; init; }

    [ClassDataSource<CustomerApiClient>(Shared = SharedType.PerTestSession)]
    public required CustomerApiClient Customer { get; init; }

    [ClassDataSource<RedisFixture>(Shared = SharedType.PerTestSession)]
    public required RedisFixture Redis { get; init; }

    [Test]
    public async Task Product_Is_Cached_After_First_Fetch()
    {
        // Create a product via admin
        var createResponse = await Admin.Client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("Cache Test Product", "electronics", 99.99m));
        var product = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // Fetch via customer (triggers caching)
        await Customer.Client.GetAsync($"/api/products/{product!.Id}");

        // Verify Redis has the cached entry
        var cached = await Redis.Database.StringGetAsync($"product:{product.Id}");
        await Assert.That(cached.HasValue).IsTrue();
    }

    [Test]
    public async Task Cache_Is_Invalidated_On_Update()
    {
        // Create and cache a product
        var createResponse = await Admin.Client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("Cache Invalidation Test", "books", 25.00m));
        var product = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();
        await Customer.Client.GetAsync($"/api/products/{product!.Id}"); // Cache it

        // Verify it's cached
        var cachedBefore = await Redis.Database.StringGetAsync($"product:{product.Id}");
        await Assert.That(cachedBefore.HasValue).IsTrue();

        // Update the product (should invalidate cache)
        await Admin.Client.PutAsJsonAsync($"/api/products/{product.Id}",
            new UpdateProductRequest(Price: 30.00m));

        // Verify cache is cleared
        var cachedAfter = await Redis.Database.StringGetAsync($"product:{product.Id}");
        await Assert.That(cachedAfter.HasValue).IsFalse();
    }

    [Test]
    public async Task Cache_Is_Invalidated_On_Delete()
    {
        // Create and cache a product
        var createResponse = await Admin.Client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("Cache Delete Test", "clothing", 45.00m));
        var product = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();
        await Customer.Client.GetAsync($"/api/products/{product!.Id}"); // Cache it

        // Delete the product
        await Admin.Client.DeleteAsync($"/api/products/{product.Id}");

        // Verify cache is cleared
        var cachedAfter = await Redis.Database.StringGetAsync($"product:{product.Id}");
        await Assert.That(cachedAfter.HasValue).IsFalse();
    }
}
