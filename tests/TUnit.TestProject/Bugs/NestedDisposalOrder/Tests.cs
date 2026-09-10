using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs.NestedDisposalOrder;

/// <summary>
/// Regression test: DisposeAsync() must be called in reverse order of InitializeAsync()
/// when using nested property injection.
///
/// Dependency chain: Tests → AppServiceFixture → AppSeedFixture → ContextFactoryFixture
/// Init order:    ContextFactoryFixture → AppSeedFixture → AppServiceFixture (deepest first)
/// Dispose order: AppServiceFixture → AppSeedFixture → ContextFactoryFixture (shallowest first = reverse)
/// </summary>

public static class NestedDisposalOrderTracker
{
    private static readonly List<string> _initOrder = [];
    private static readonly List<string> _disposeOrder = [];
    private static readonly Lock _lock = new();

    public static void RecordInit(string name)
    {
        lock (_lock)
        {
            _initOrder.Add(name);
        }
    }

    public static void RecordDispose(string name)
    {
        lock (_lock)
        {
            _disposeOrder.Add(name);
        }
    }

    public static IReadOnlyList<string> GetInitOrder()
    {
        lock (_lock)
        {
            return _initOrder.ToList();
        }
    }

    public static IReadOnlyList<string> GetDisposeOrder()
    {
        lock (_lock)
        {
            return _disposeOrder.ToList();
        }
    }

    public static void Reset()
    {
        lock (_lock)
        {
            _initOrder.Clear();
            _disposeOrder.Clear();
        }
    }
}

public sealed class ContextFactoryFixture2 : IAsyncInitializer, IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        NestedDisposalOrderTracker.RecordDispose(nameof(ContextFactoryFixture2));
        return ValueTask.CompletedTask;
    }

    public Task InitializeAsync()
    {
        NestedDisposalOrderTracker.RecordInit(nameof(ContextFactoryFixture2));
        return Task.CompletedTask;
    }
}

public sealed class AppSeedFixture2 : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<ContextFactoryFixture2>(Shared = SharedType.PerTestSession)]
    public required ContextFactoryFixture2 ContextFactoryFixture { get; init; }

    public ValueTask DisposeAsync()
    {
        NestedDisposalOrderTracker.RecordDispose(nameof(AppSeedFixture2));
        return ValueTask.CompletedTask;
    }

    public Task InitializeAsync()
    {
        NestedDisposalOrderTracker.RecordInit(nameof(AppSeedFixture2));
        return Task.CompletedTask;
    }
}

public sealed class AppServiceFixture2 : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<AppSeedFixture2>(Shared = SharedType.PerTestSession)]
    public required AppSeedFixture2 AppSeedFixture { get; init; }

    public ValueTask DisposeAsync()
    {
        NestedDisposalOrderTracker.RecordDispose(nameof(AppServiceFixture2));
        return ValueTask.CompletedTask;
    }

    public Task InitializeAsync()
    {
        NestedDisposalOrderTracker.RecordInit(nameof(AppServiceFixture2));
        return Task.CompletedTask;
    }
}

[NotInParallel]
[EngineTest(ExpectedResult.Pass)]
public class NestedDisposalOrderTests
{
    [ClassDataSource<AppServiceFixture2>(Shared = SharedType.PerTestSession)]
    public required AppServiceFixture2 AppServiceFixture { get; init; }

    [Before(Class)]
    public static void ResetTrackers()
    {
        NestedDisposalOrderTracker.Reset();
    }

    [Test]
    public async Task Test1()
    {
        await Assert.That(true).IsTrue();
    }

    [After(TestSession)]
#pragma warning disable TUnit0042
    public static async Task VerifyDisposalOrder(TestSessionContext context)
#pragma warning restore TUnit0042
    {
        var initOrder = NestedDisposalOrderTracker.GetInitOrder();
        var disposeOrder = NestedDisposalOrderTracker.GetDisposeOrder();

        // Guard: skip assertions if this test class was not part of the test run
        if (initOrder.Count == 0)
        {
            return;
        }

        Console.WriteLine($"Init order: {string.Join(" -> ", initOrder)}");
        Console.WriteLine($"Dispose order: {string.Join(" -> ", disposeOrder)}");

        // Init should be deepest first
        await Assert.That(initOrder).Count().IsEqualTo(3);
        await Assert.That(initOrder[0]).IsEqualTo(nameof(ContextFactoryFixture2))
            .Because("deepest dependency should be initialized first");
        await Assert.That(initOrder[1]).IsEqualTo(nameof(AppSeedFixture2))
            .Because("middle dependency should be initialized second");
        await Assert.That(initOrder[2]).IsEqualTo(nameof(AppServiceFixture2))
            .Because("top-level dependency should be initialized last");

        // Dispose should be reverse of init (shallowest first)
        await Assert.That(disposeOrder).Count().IsEqualTo(3);
        await Assert.That(disposeOrder[0]).IsEqualTo(nameof(AppServiceFixture2))
            .Because("top-level (shallowest) should be disposed first");
        await Assert.That(disposeOrder[1]).IsEqualTo(nameof(AppSeedFixture2))
            .Because("middle dependency should be disposed second");
        await Assert.That(disposeOrder[2]).IsEqualTo(nameof(ContextFactoryFixture2))
            .Because("deepest dependency should be disposed last");
    }
}
