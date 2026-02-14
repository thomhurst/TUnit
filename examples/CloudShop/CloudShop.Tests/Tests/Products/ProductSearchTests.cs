using System.Net.Http.Json;
using CloudShop.Shared.Contracts;
using CloudShop.Tests.Infrastructure;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace CloudShop.Tests.Tests.Products;

/// <summary>
/// Tests product search with combinatorial parameters using MatrixDataSource.
///
/// Showcases:
/// - [MatrixDataSource] generating all combinations automatically
/// - [Matrix] attribute on parameters for discrete values
/// - 27 test cases generated from just 3 parameters (3 × 3 × 3)
/// - [Category] for test organization
/// </summary>
[Category("Integration"), Category("Search")]
public class ProductSearchTests
{
    [ClassDataSource<CustomerApiClient>(Shared = SharedType.PerTestSession)]
    public required CustomerApiClient Client { get; init; }

    [Test]
    [MatrixDataSource]
    public async Task Search_Returns_Correct_Filtered_Results(
        [Matrix("electronics", "clothing", "books")] string category,
        [Matrix("price_asc", "price_desc", "name")] string sortBy,
        [Matrix(10, 25, 50)] int pageSize)
    {
        var response = await Client.Client.GetFromJsonAsync<PagedResult<ProductResponse>>(
            $"/api/products?category={category}&sort={sortBy}&pageSize={pageSize}");

        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Items.Count).IsLessThanOrEqualTo(pageSize);
        await Assert.That(response.PageSize).IsEqualTo(pageSize);

        // All returned items should match the category filter
        foreach (var item in response.Items)
        {
            await Assert.That(item.Category.ToLowerInvariant()).IsEqualTo(category);
        }
    }

    [Test]
    [MatrixDataSource]
    public async Task Pagination_Works_Correctly(
        [Matrix(1, 2, 3)] int page,
        [Matrix(2, 5)] int pageSize)
    {
        var response = await Client.Client.GetFromJsonAsync<PagedResult<ProductResponse>>(
            $"/api/products?page={page}&pageSize={pageSize}");

        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Items.Count).IsLessThanOrEqualTo(pageSize);
        await Assert.That(response.Page).IsEqualTo(page);
    }
}
