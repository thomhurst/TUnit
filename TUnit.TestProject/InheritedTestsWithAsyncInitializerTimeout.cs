using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

// Data source that implements IAsyncInitializer and IAsyncDisposable
public class AsyncInitializableDataSource : IAsyncInitializer, IAsyncDisposable
{
    private bool _initialized;
    
    public int Value { get; private set; }

    public async Task InitializeAsync()
    {
        // Simulate async initialization that could potentially hang
        await Task.Delay(100);
        _initialized = true;
        Value = 42;
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Delay(10);
        _initialized = false;
    }
}

// Base class with tests that use async initializable data sources
public abstract class BaseTestWithAsyncInitializer
{
    private readonly AsyncInitializableDataSource _dataSource;

    protected BaseTestWithAsyncInitializer(AsyncInitializableDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    [Test]
    [Timeout(3000)] // 3 second timeout
    public async Task TestWithAsyncInitializableDataSource()
    {
        await Console.Out.WriteLineAsync($"Running test with async initializable data source: {_dataSource.Value}");
        await Assert.That(_dataSource.Value).IsEqualTo(42);
    }

    [Test]
    [Timeout(2000)] // 2 second timeout
    public async Task AnotherTestWithTimeout()
    {
        await Console.Out.WriteLineAsync("Running another test with timeout");
        await Task.Delay(500); // Should complete successfully
        await Assert.That(_dataSource.Value).IsEqualTo(42);
    }
}

// Derived class A with InheritsTests and class data source
[InheritsTests]
[ClassDataSource<AsyncInitializableDataSource>(Shared = SharedType.PerTestClass)]
public sealed class DerivedTestWithAsyncInitializerA(AsyncInitializableDataSource dataSource) 
    : BaseTestWithAsyncInitializer(dataSource)
{
}

// Derived class B with InheritsTests and class data source
[InheritsTests]
[ClassDataSource<AsyncInitializableDataSource>(Shared = SharedType.PerTestClass)]
public sealed class DerivedTestWithAsyncInitializerB(AsyncInitializableDataSource dataSource) 
    : BaseTestWithAsyncInitializer(dataSource)
{
}

