using System.Net.Http.Json;
using CloudShop.Shared.Contracts;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace CloudShop.Tests.Infrastructure;

/// <summary>
/// Seeds additional test data for specific test classes.
/// Shared per class - each test class gets its own seeded data.
/// Nested dependency: injects AdminApiClient to create data via the API.
/// </summary>
public class SeededDatabaseFixture : IAsyncInitializer
{
    [ClassDataSource<AdminApiClient>(Shared = SharedType.PerTestSession)]
    public required AdminApiClient Admin { get; init; }

    public List<ProductResponse> SeededProducts { get; } = [];

    public async Task InitializeAsync()
    {
        // Seed extra test products via the API
        var testProducts = new[]
        {
            new CreateProductRequest("Test Widget Alpha", "electronics", 19.99m, "Alpha test product", 50),
            new CreateProductRequest("Test Widget Beta", "electronics", 29.99m, "Beta test product", 75),
            new CreateProductRequest("Test Book Gamma", "books", 12.99m, "Gamma test book", 100),
            new CreateProductRequest("Test Shirt Delta", "clothing", 34.99m, "Delta test shirt", 200),
        };

        foreach (var product in testProducts)
        {
            var response = await Admin.Client.PostAsJsonAsync("/api/products", product);
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<ProductResponse>();
                if (created is not null)
                    SeededProducts.Add(created);
            }
        }
    }
}
