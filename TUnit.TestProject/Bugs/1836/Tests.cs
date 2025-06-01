using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._1836;

[NotInParallel]
public class TestDbContext : IAsyncInitializer, IAsyncDisposable, ITestEndEventReceiver
{
    private bool _isConnectionOpen;

    public string[] FetchThings()
    {
        if (!_isConnectionOpen)
        {
            throw new InvalidOperationException();
        }

        // Retrieve things from the database.
        return [];
    }

    public Task InitializeAsync()
    {
        _isConnectionOpen = true;
        return Task.CompletedTask;
    }

    public ValueTask OnTestEnd(AfterTestContext testContext)
    {
        if (!_isConnectionOpen)
        {
            throw new InvalidOperationException();
        }

        // Delete all rows in the things table.
        return default;
    }

    public ValueTask DisposeAsync()
    {
        _isConnectionOpen = false;
        return default;
    }

    public int Order { get; }
}

[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    [ClassDataSource<TestDbContext>(Shared = SharedType.PerTestSession)]
    public required TestDbContext DbContext { get; init; }

    [Test]
    public async Task EmptyThings()
    {
        await Assert.That(DbContext.FetchThings()).IsEmpty();
    }
}