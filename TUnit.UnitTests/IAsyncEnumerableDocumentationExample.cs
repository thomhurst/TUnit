using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.UnitTests;

public record AsyncTestData(int Id, string Name, DateTime CreatedAt);

public static class AsyncTestDataSources
{
    // AOT-compatible async data source with cancellation support - matching documentation
    public static async IAsyncEnumerable<Func<AsyncTestData>> GetAsyncTestData(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        for (var i = 1; i <= 3; i++)
        {
            ct.ThrowIfCancellationRequested();

            // Simulate async data loading (database, API, etc.)
            await Task.Delay(10, ct);

            yield return () => new AsyncTestData(
                Id: i,
                Name: $"Item_{i}",
                CreatedAt: DateTime.UtcNow.AddDays(-i)
            );
        }
    }

    // Simple async enumerable returning tuples - matching documentation
    public static async IAsyncEnumerable<(int, string)> GetSimpleAsyncData(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.Delay(1, ct); // Simulate async work
        yield return (1, "first");
        yield return (2, "second");
        yield return (3, "third");
    }
}

public class IAsyncEnumerableDocumentationExample
{
    [Test]
    [MethodDataSource(typeof(AsyncTestDataSources), nameof(AsyncTestDataSources.GetAsyncTestData))]
    public async Task TestWithAsyncComplexData(AsyncTestData testData)
    {
        await Assert.That(testData.Id).IsGreaterThan(0);
        await Assert.That(testData.Name).StartsWith("Item_");
        await Assert.That(testData.CreatedAt).IsLessThan(DateTime.UtcNow);
    }

    [Test]
    [MethodDataSource(typeof(AsyncTestDataSources), nameof(AsyncTestDataSources.GetSimpleAsyncData))]
    public async Task TestWithAsyncSimpleData(int id, string name)
    {
        await Assert.That(id).IsGreaterThan(0).And.IsLessThanOrEqualTo(3);
        await Assert.That(name).IsNotEmpty();
    }
}
