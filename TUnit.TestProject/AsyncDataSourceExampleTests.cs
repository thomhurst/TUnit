using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class AsyncApiDataSource : AsyncDataSourceGeneratorAttribute<string>
{
    protected override async IAsyncEnumerable<Func<Task<string>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Simulate API calls
        yield return async () =>
        {
            var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(10)); // Simulate network delay
            return "API Result 1";
        };

        yield return async () =>
        {
            var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(10)); // Simulate network delay
            return "API Result 2";
        };

        yield return async () =>
        {
            var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(10)); // Simulate network delay
            return "API Result 3";
        };
    }
}

public class AsyncUserDataSource : AsyncDataSourceGeneratorAttribute<int, string>
{
    protected override async IAsyncEnumerable<Func<Task<(int, string)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Simulate fetching users from a database
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(5)); // Simulate initial DB connection

        yield return async () =>
        {
            var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(5)); // Simulate query delay
            return (1, "Alice");
        };

        yield return async () =>
        {
            var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(5)); // Simulate query delay
            return (2, "Bob");
        };

        yield return async () =>
        {
            var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(5)); // Simulate query delay
            return (3, "Charlie");
        };
    }
}

public class AsyncUntypedDataSource : AsyncUntypedDataSourceGeneratorAttribute
{
    protected override async IAsyncEnumerable<Func<Task<object?[]?>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Simulate various async data sources
        yield return async () =>
        {
            var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(10));
            return [42, "Answer", true];
        };

        yield return async () =>
        {
            var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(10));
            return [100, "Century", false];
        };
    }
}

public class AsyncDataSourceExampleTests
{
    [Test]
    [AsyncApiDataSource]
    public async Task TestWithAsyncApiData(string apiResult)
    {
        // Test would use the async data from the API
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(1)); // Simulate some async work
        await Assert.That(apiResult).IsNotNull();
        await Assert.That(apiResult).Contains("API Result");
    }

    [Test]
    [AsyncUserDataSource]
    public async Task TestWithAsyncUserData(int userId, string userName)
    {
        // Test would use the async user data
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(1)); // Simulate some async work
        await Assert.That(userId).IsGreaterThan(0);
        await Assert.That(userName).IsNotEmpty();
    }

    [Test]
    [AsyncUntypedDataSource]
    public async Task TestWithAsyncUntypedData(int number, string text, bool flag)
    {
        // Test would use the async non-typed data
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(1)); // Simulate some async work
        await Assert.That(number).IsGreaterThan(0);
        await Assert.That(text).IsNotEmpty();
        // flag is already strongly typed as bool from the async data source
    }
}
