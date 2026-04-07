using Azure.Data.Tables;
using Azure.Storage.Blobs;

namespace TUnit.Mocks.Tests;

// Reproduction for https://github.com/thomhurst/TUnit/issues/5434
// BlobClient: CS0111 duplicate GenerateSasUri / GenerateUserDelegationSasUri members in generated extensions.
// TableClient: CS0411 type inference failures for generic methods (GetEntity<T>, GetEntityAsync<T>,
// GetEntityIfExists<T>, GetEntityIfExistsAsync<T>, Query<T>, QueryAsync<T>) in generated impl factory.
public class Issue5434Tests
{
    [Test]
    public void Can_Mock_BlobClient()
    {
        var mock = Mock.Of<BlobClient>(MockBehavior.Strict);
        _ = mock.Object;
    }

    [Test]
    public void Can_Mock_TableClient()
    {
        var mock = Mock.Of<TableClient>(MockBehavior.Strict);
        _ = mock.Object;
    }
}
