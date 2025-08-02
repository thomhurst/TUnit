namespace TUnit.UnitTests;

public class AsyncDataSourceTests
{
    // Sync data source for comparison
    public static IEnumerable<object[]> SyncDataSource()
    {
        yield return [1, "one"];
        yield return [2, "two"];
        yield return [3, "three"];
    }

    // Async data source returning Task<IEnumerable<T>>
    public static async Task<IEnumerable<object[]>> AsyncTaskDataSource()
    {
        await Task.Delay(10); // Simulate async work
        return new List<object[]>
        {
            new object[] { 10, "ten" },
            new object[] { 20, "twenty" },
            new object[] { 30, "thirty" }
        };
    }

    // Async data source returning ValueTask<IEnumerable<T>>
    public static async ValueTask<IEnumerable<object[]>> AsyncValueTaskDataSource()
    {
        await Task.Delay(5); // Simulate async work
        return new List<object[]>
        {
            new object[] { 100, "hundred" },
            new object[] { 200, "two hundred" }
        };
    }

    // Async enumerable data source
    public static async IAsyncEnumerable<object[]> AsyncEnumerableDataSource()
    {
        await Task.Delay(1);
        yield return [1000, "thousand"];

        await Task.Delay(1);
        yield return [2000, "two thousand"];

        await Task.Delay(1);
        yield return [3000, "three thousand"];
    }


    [Test]
    [MethodDataSource(nameof(SyncDataSource))]
    public async Task TestWithSyncDataSource(int value, string text)
    {
        await Assert.That(value).IsGreaterThan(0);
        await Assert.That(text).IsNotNull();
        await Assert.That(text).IsNotEmpty();
    }

    [Test]
    [MethodDataSource(nameof(AsyncTaskDataSource))]
    public async Task TestWithAsyncTaskDataSource(int value, string text)
    {
        await Assert.That(value).IsGreaterThan(0);
        await Assert.That(text).IsNotNull();
        await Assert.That(text).Contains("t");
    }

    [Test]
    [MethodDataSource(nameof(AsyncValueTaskDataSource))]
    public async Task TestWithAsyncValueTaskDataSource(int value, string text)
    {
        await Assert.That(value).IsGreaterThanOrEqualTo(100);
        await Assert.That(text).IsNotNull();
        await Assert.That(text).Contains("hundred");
    }


}

