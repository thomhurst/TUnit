using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._5118;

public class AsyncTestDataSources
{
    public static async IAsyncEnumerable<(int Id, string Name, DateTime CreatedAt)> GetAsyncTestData()
    {
        for (var i = 1; i <= 3; i++)
        {
            await Task.Delay(10).ConfigureAwait(false);

            yield return (Id: i, Name: $"Item_{i}", CreatedAt: DateTime.UtcNow.AddDays(-i));
        }
    }
}

[EngineTest(ExpectedResult.Pass)]
[MethodDataSource<AsyncTestDataSources>(nameof(AsyncTestDataSources.GetAsyncTestData))]
public class AsyncClassMethodDataSourceTests(int Id, string Name, DateTime CreatedAt)
{
    [Test, Timeout(5000)]
    public async Task TestWithAsyncComplexData(CancellationToken token)
    {
        await Assert.That(Id).IsGreaterThan(0);
        await Assert.That(Name).StartsWith("Item_");
        await Assert.That(CreatedAt).IsLessThan(DateTime.UtcNow);
    }
}
