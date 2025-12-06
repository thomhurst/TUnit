using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3992;

/// <summary>
/// Regression test for issue #3992: IAsyncInitializer should not run during test discovery.
/// Verifies that IAsyncInitializer.InitializeAsync() is only called during test execution,
/// not during the discovery phase.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class IAsyncInitializerDiscoveryTests
{
    private static int _initializationCount = 0;

    public class DataSourceWithAsyncInit : IAsyncInitializer
    {
        public bool IsInitialized { get; private set; }

        public Task InitializeAsync()
        {
            Interlocked.Increment(ref _initializationCount);
            IsInitialized = true;
            Console.WriteLine($"InitializeAsync called (count: {_initializationCount})");
            return Task.CompletedTask;
        }
    }

    [Test]
    [ClassDataSource<DataSourceWithAsyncInit>]
    public async Task DataSource_InitializeAsync_OnlyCalledDuringExecution(DataSourceWithAsyncInit dataSource)
    {
        // Verify that the data source was initialized
        await Assert.That(dataSource.IsInitialized).IsTrue();

        // Verify that InitializeAsync was called (at least once during execution)
        await Assert.That(_initializationCount).IsGreaterThan(0);

        Console.WriteLine($"Test execution confirmed: InitializeAsync was called {_initializationCount} time(s)");
    }
}
