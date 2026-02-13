using CloudShop.Shared.Contracts;

namespace CloudShop.Tests.DataSources;

/// <summary>
/// Provides test data for product-related tests using [MethodDataSource].
/// Returns Func&lt;T&gt; for reference types to ensure proper test isolation (TUnit best practice).
/// </summary>
public static class ProductDataSources
{
    /// <summary>
    /// Valid product creation requests for parameterized tests.
    /// Usage: [MethodDataSource(typeof(ProductDataSources), nameof(ValidProducts))]
    /// </summary>
    public static IEnumerable<Func<CreateProductRequest>> ValidProducts()
    {
        yield return () => new("Bluetooth Speaker", "electronics", 39.99m, "Portable wireless speaker", 200);
        yield return () => new("Yoga Mat", "clothing", 29.99m, "Non-slip exercise mat", 150);
        yield return () => new("Cooking Guide", "books", 19.99m, "100 easy recipes", 300);
        yield return () => new("Budget Item", "electronics", 0.99m, "Cheapest item", 1);
        yield return () => new("Premium Watch", "electronics", 999.99m, "Luxury smartwatch", 10);
    }

    /// <summary>
    /// All product categories used in the test seed data.
    /// </summary>
    public static IEnumerable<string> Categories()
    {
        yield return "electronics";
        yield return "clothing";
        yield return "books";
    }

    /// <summary>
    /// Valid product update requests for testing partial updates.
    /// Usage: [MethodDataSource(typeof(ProductDataSources), nameof(ValidUpdates))]
    /// </summary>
    public static IEnumerable<Func<UpdateProductRequest>> ValidUpdates()
    {
        yield return () => new(Name: "Updated Name");
        yield return () => new(Price: 99.99m);
        yield return () => new(Category: "sports");
        yield return () => new(StockQuantity: 500);
        yield return () => new(Description: "Updated description");
        yield return () => new(Name: "Full Update", Category: "electronics", Price: 49.99m, Description: "All fields", StockQuantity: 100);
    }
}
