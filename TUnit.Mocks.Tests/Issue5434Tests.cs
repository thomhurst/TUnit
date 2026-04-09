using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace TUnit.Mocks.Tests;

// Reproduction and regression tests for https://github.com/thomhurst/TUnit/issues/5434
// BlobClient: CS0111 duplicate GenerateSasUri / GenerateUserDelegationSasUri members in generated extensions.
// TableClient: CS0411 type inference failures for generic methods (GetEntity<T>, GetEntityAsync<T>,
// GetEntityIfExists<T>, GetEntityIfExistsAsync<T>, Query<T>, QueryAsync<T>) in generated impl factory.
public class Issue5434Tests
{
    [Test]
    public void Can_Mock_BlobClient()
    {
        var mock = BlobClient.Mock(MockBehavior.Strict);
        _ = mock.Object;
    }

    [Test]
    public void Can_Mock_TableClient()
    {
        var mock = TableClient.Mock(MockBehavior.Strict);
        _ = mock.Object;
    }

    // Exercises the disambiguated overload that keeps `out string stringToSign` in its
    // signature to distinguish it from GenerateSasUri(perms, expires). This call would not
    // compile if `keepOutParams` disambiguation regressed.
    [Test]
    public void Can_Configure_BlobClient_GenerateSasUri_OutOverload()
    {
        var mock = BlobClient.Mock(MockBehavior.Loose);
        _ = mock.GenerateSasUri(Arg.Any<BlobSasPermissions>(), Arg.Any<System.DateTimeOffset>(), out _);
    }

    // Exercises the generic-return-type override path. This would not compile if
    // the base.GetEntity(...) call in the generated override was missing the <T> type argument.
    [Test]
    public void Can_Configure_TableClient_GetEntity_Generic()
    {
        var mock = TableClient.Mock(MockBehavior.Loose);
        _ = mock.GetEntity<TableEntity>(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<System.Collections.Generic.IEnumerable<string>>(),
            Arg.Any<System.Threading.CancellationToken>());
    }
}
