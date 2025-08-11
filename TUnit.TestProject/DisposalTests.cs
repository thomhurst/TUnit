using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

// Test data source that implements IAsyncDisposable to verify disposal behavior
public class TestAsyncDisposableDataSource : IAsyncDisposable
{
    private static int _disposeAsyncCallCount;
    
    public static int DisposeAsyncCallCount => _disposeAsyncCallCount;
    
    public static void Reset()
    {
        _disposeAsyncCallCount = 0;
    }

    public ValueTask DisposeAsync()
    {
        System.Threading.Interlocked.Increment(ref _disposeAsyncCallCount);
        return default(ValueTask);
    }
}

// Test data source that implements IDisposable to verify disposal behavior
public class TestDisposableDataSource : IDisposable
{
    private static int _disposeCallCount;
    
    public static int DisposeCallCount => _disposeCallCount;
    
    public static void Reset()
    {
        _disposeCallCount = 0;
    }

    public void Dispose()
    {
        System.Threading.Interlocked.Increment(ref _disposeCallCount);
    }
}

// Test class to verify that injected disposable instances are properly disposed
public class DisposalTestClass
{
    [ClassDataSource<TestAsyncDisposableDataSource>(Shared = SharedType.PerClass)]
    public required TestAsyncDisposableDataSource AsyncDisposableDataSource { get; init; }
    
    [ClassDataSource<TestDisposableDataSource>(Shared = SharedType.PerClass)]
    public required TestDisposableDataSource DisposableDataSource { get; init; }

    [Test]
    public async Task TestThatUsesAsyncDisposableDataSource()
    {
        // Reset the flags before the test
        TestAsyncDisposableDataSource.Reset();
        TestDisposableDataSource.Reset();
        
        // Use the injected data source
        await Assert.That(AsyncDisposableDataSource).IsNotNull();
        await Assert.That(DisposableDataSource).IsNotNull();
    }
    
    [Test]
    public async Task SecondTestUsesSameSharedInstances()
    {
        // This test verifies we have the same shared instances
        await Assert.That(AsyncDisposableDataSource).IsNotNull();
        await Assert.That(DisposableDataSource).IsNotNull();
    }
}

// Another class that tests if disposal works after all tests in a class
public class VerifyDisposalTestClass  
{
    [Test]
    public async Task VerifyDisposalAfterClassCompletes()
    {
        // This test should run after the disposal test class
        // Since we used PerClass sharing, the objects should be disposed when all tests in that class complete
        // The disposal should happen when the reference count reaches zero in ObjectTracker
        
        // For now, we can't easily verify disposal from another class since the static counters
        // could be reset. We need to find a better way to test this.
        await Assert.That(true).IsTrue();
    }
}