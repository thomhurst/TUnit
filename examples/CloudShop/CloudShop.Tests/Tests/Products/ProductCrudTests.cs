using System.Net.Http.Json;
using CloudShop.Shared.Contracts;
using CloudShop.Tests.DataSources;
using CloudShop.Tests.Infrastructure;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace CloudShop.Tests.Tests.Products;

/// <summary>
/// Tests product CRUD operations using ClassDataSource for API clients
/// and MethodDataSource for test data.
///
/// Showcases:
/// - [ClassDataSource] with SharedType.PerTestSession for fixture sharing
/// - [MethodDataSource] for parameterized test data
/// - [Category] for test filtering
/// - Built-in HTTP response assertions
/// </summary>
[Category("Integration"), Category("Products")]
public class ProductCrudTests
{
    [ClassDataSource<AdminApiClient>(Shared = SharedType.PerTestSession)]
    public required AdminApiClient Admin { get; init; }

    [ClassDataSource<CustomerApiClient>(Shared = SharedType.PerTestSession)]
    public required CustomerApiClient Customer { get; init; }

    [Test]
    [MethodDataSource(typeof(ProductDataSources), nameof(ProductDataSources.ValidProducts))]
    public async Task Admin_Can_Create_Product(CreateProductRequest product)
    {
        var response = await Admin.Client.PostAsJsonAsync("/api/products", product);

        await Assert.That(response).IsCreated();

        var created = await response.Content.ReadFromJsonAsync<ProductResponse>();
        await Assert.That(created).IsNotNull();
        await Assert.That(created!.Name).IsEqualTo(product.Name);
        await Assert.That(created.Category).IsEqualTo(product.Category);
        await Assert.That(created.Price).IsEqualTo(product.Price);
    }

    [Test]
    [MethodDataSource(typeof(ProductDataSources), nameof(ProductDataSources.ValidProducts))]
    public async Task Customer_Cannot_Create_Product(CreateProductRequest product)
    {
        var response = await Customer.Client.PostAsJsonAsync("/api/products", product);

        await Assert.That(response).IsForbidden();
    }

    [Test]
    public async Task Can_Get_Product_By_Id()
    {
        // Create a product first
        var createResponse = await Admin.Client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("GetById Test Product", "electronics", 15.99m));
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // Fetch it
        var response = await Customer.Client.GetAsync($"/api/products/{created!.Id}");

        await Assert.That(response).IsSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        await Assert.That(product!.Name).IsEqualTo("GetById Test Product");
    }

    [Test]
    [MethodDataSource(typeof(ProductDataSources), nameof(ProductDataSources.ValidUpdates))]
    public async Task Admin_Can_Update_Product(UpdateProductRequest update)
    {
        // Create a product first
        var createResponse = await Admin.Client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("Update Test", "books", 10.00m));
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // Update it
        var response = await Admin.Client.PutAsJsonAsync($"/api/products/{created!.Id}", update);

        await Assert.That(response).IsSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<ProductResponse>();
        await Assert.That(updated).IsNotNull();

        // Verify only specified fields changed
        if (update.Name is not null)
            await Assert.That(updated!.Name).IsEqualTo(update.Name);
        if (update.Price.HasValue)
            await Assert.That(updated!.Price).IsEqualTo(update.Price.Value);
    }

    [Test]
    public async Task Admin_Can_Delete_Product()
    {
        // Create a product
        var createResponse = await Admin.Client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("Delete Me", "books", 5.00m));
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // Delete it
        var deleteResponse = await Admin.Client.DeleteAsync($"/api/products/{created!.Id}");
        await Assert.That(deleteResponse).IsNoContent();

        // Verify it's gone
        var getResponse = await Customer.Client.GetAsync($"/api/products/{created.Id}");
        await Assert.That(getResponse).IsNotFound();
    }

    [Test]
    public async Task Customer_Cannot_Delete_Product()
    {
        var response = await Customer.Client.DeleteAsync("/api/products/1");
        await Assert.That(response).IsForbidden();
    }
}
