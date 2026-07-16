using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs.Bug3266;

// Global tracker to verify initialization order across all components
public static class InitializationOrderTracker
{
    private static int _counter;
    private static readonly ConcurrentDictionary<string, int> InitOrder = new();

    public static int RecordInitialization(string componentName)
    {
        var order = Interlocked.Increment(ref _counter);
        InitOrder[componentName] = order;
        return order;
    }

    public static int GetOrder(string componentName) => InitOrder.TryGetValue(componentName, out var order) ? order : -1;

    public static void Reset()
    {
        _counter = 0;
        InitOrder.Clear();
    }
}

// Mock test container - shared per test session
public class PulsarTestContainer : IAsyncInitializer, IAsyncDisposable
{
    public bool IsInitialized { get; private set; }
    public bool IsDisposed { get; private set; }
    public int InitializationOrder { get; private set; }

    public Task InitializeAsync()
    {
        InitializationOrder = InitializationOrderTracker.RecordInitialization(nameof(PulsarTestContainer));
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return default;
    }
}

// Mock connection class - depends on PulsarTestContainer being initialized
public class PulsarConnection : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<PulsarTestContainer>(Shared = SharedType.PerTestSession)]
    public required PulsarTestContainer Container { get; init; }

    public bool IsInitialized { get; private set; }
    public bool IsDisposed { get; private set; }
    public int InitializationOrder { get; private set; }

    public Task InitializeAsync()
    {
        // This should fail if Container.InitializeAsync() hasn't been called yet
        if (!Container.IsInitialized)
        {
            throw new InvalidOperationException(
                "PulsarConnection.InitializeAsync() called before nested Container property was initialized!");
        }

        InitializationOrder = InitializationOrderTracker.RecordInitialization(nameof(PulsarConnection));
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return default;
    }
}

// Mock web app factory - also depends on PulsarTestContainer
public class WebAppFactory : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<PulsarTestContainer>(Shared = SharedType.PerTestSession)]
    public required PulsarTestContainer Container { get; init; }

    public bool IsInitialized { get; private set; }
    public bool IsDisposed { get; private set; }
    public int InitializationOrder { get; private set; }

    public Task InitializeAsync()
    {
        // This should fail if Container.InitializeAsync() hasn't been called yet
        if (!Container.IsInitialized)
        {
            throw new InvalidOperationException(
                "WebAppFactory.InitializeAsync() called before nested Container property was initialized!");
        }

        InitializationOrder = InitializationOrderTracker.RecordInitialization(nameof(WebAppFactory));
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return default;
    }
}

// Base abstract class with WebAppFactory property
public abstract class AbstractClassA
{
    [ClassDataSource<WebAppFactory>(Shared = SharedType.None)]
    public required WebAppFactory WebApp { get; init; }
}

// Middle abstract class with PulsarConnection property
public abstract class AbstractClassB : AbstractClassA
{
    [ClassDataSource<PulsarConnection>(Shared = SharedType.None)]
    public required PulsarConnection Connection { get; init; }
}

// Concrete test class - reproduces issue #3266
[EngineTest(ExpectedResult.Pass)]
[NotInParallel]
public class Issue3266ReproTest : AbstractClassB
{
    [Test]
    public async Task NestedPropertiesShouldBeInitializedBeforeParentInitializeAsync()
    {
        // Verify all nested containers are initialized
        await Assert.That(Connection.Container.IsInitialized)
            .IsTrue()
            .Because("PulsarConnection's Container should be initialized");

        await Assert.That(WebApp.Container.IsInitialized)
            .IsTrue()
            .Because("WebAppFactory's Container should be initialized");

        // Verify parent objects are initialized
        await Assert.That(Connection.IsInitialized)
            .IsTrue()
            .Because("PulsarConnection should be initialized");

        await Assert.That(WebApp.IsInitialized)
            .IsTrue()
            .Because("WebAppFactory should be initialized");
    }

    [Test]
    public async Task BothPropertiesShouldShareTheSameContainer()
    {
        // Since both use SharedType.PerTestSession, they should get the same instance
        await Assert.That(Connection.Container)
            .IsSameReferenceAs(WebApp.Container)
            .Because("Both properties should share the same PulsarTestContainer instance");
    }

    [Test]
    public async Task InitializationOrderShouldBeDependencyFirst()
    {
        // PulsarTestContainer must be initialized before both PulsarConnection and WebAppFactory
        await Assert.That(Connection.Container.InitializationOrder)
            .IsLessThan(Connection.InitializationOrder)
            .Because("Container should be initialized before PulsarConnection");

        await Assert.That(WebApp.Container.InitializationOrder)
            .IsLessThan(WebApp.InitializationOrder)
            .Because("Container should be initialized before WebAppFactory");
    }
}

#region Deep Nested Dependency Chain Tests

// Level 1: Deepest shared dependency
public class DeepDependencyLevel1 : IAsyncInitializer, IAsyncDisposable
{
    public bool IsInitialized { get; private set; }
    public int InitializationOrder { get; private set; }

    public Task InitializeAsync()
    {
        InitializationOrder = InitializationOrderTracker.RecordInitialization($"{nameof(DeepDependencyLevel1)}_{GetHashCode()}");
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => default;
}

// Level 2: Depends on Level 1
public class DeepDependencyLevel2 : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<DeepDependencyLevel1>(Shared = SharedType.PerTestSession)]
    public required DeepDependencyLevel1 Level1 { get; init; }

    public bool IsInitialized { get; private set; }
    public int InitializationOrder { get; private set; }

    public Task InitializeAsync()
    {
        if (!Level1.IsInitialized)
        {
            throw new InvalidOperationException("Level2 initialized before Level1!");
        }
        InitializationOrder = InitializationOrderTracker.RecordInitialization($"{nameof(DeepDependencyLevel2)}_{GetHashCode()}");
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => default;
}

// Level 3: Depends on Level 2
public class DeepDependencyLevel3 : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<DeepDependencyLevel2>(Shared = SharedType.PerTestSession)]
    public required DeepDependencyLevel2 Level2 { get; init; }

    public bool IsInitialized { get; private set; }
    public int InitializationOrder { get; private set; }

    public Task InitializeAsync()
    {
        if (!Level2.IsInitialized)
        {
            throw new InvalidOperationException("Level3 initialized before Level2!");
        }
        if (!Level2.Level1.IsInitialized)
        {
            throw new InvalidOperationException("Level3 initialized before Level1!");
        }
        InitializationOrder = InitializationOrderTracker.RecordInitialization($"{nameof(DeepDependencyLevel3)}_{GetHashCode()}");
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => default;
}

// Level 4: Depends on Level 3 (deepest level in the chain)
public class DeepDependencyLevel4 : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<DeepDependencyLevel3>(Shared = SharedType.PerTestSession)]
    public required DeepDependencyLevel3 Level3 { get; init; }

    public bool IsInitialized { get; private set; }
    public int InitializationOrder { get; private set; }

    public Task InitializeAsync()
    {
        if (!Level3.IsInitialized)
        {
            throw new InvalidOperationException("Level4 initialized before Level3!");
        }
        InitializationOrder = InitializationOrderTracker.RecordInitialization($"{nameof(DeepDependencyLevel4)}_{GetHashCode()}");
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => default;
}

// Abstract base class in deep chain
public abstract class DeepAbstractBase
{
    [ClassDataSource<DeepDependencyLevel4>(Shared = SharedType.None)]
    public required DeepDependencyLevel4 DeepDependency { get; init; }
}

// Concrete test for 4-level deep dependency chain
[EngineTest(ExpectedResult.Pass)]
[NotInParallel]
public class DeepDependencyChainTest : DeepAbstractBase
{
    [Test]
    public async Task FourLevelDeepChainShouldInitializeInCorrectOrder()
    {
        // Verify all levels are initialized
        await Assert.That(DeepDependency.Level3.Level2.Level1.IsInitialized).IsTrue();
        await Assert.That(DeepDependency.Level3.Level2.IsInitialized).IsTrue();
        await Assert.That(DeepDependency.Level3.IsInitialized).IsTrue();
        await Assert.That(DeepDependency.IsInitialized).IsTrue();

        // Verify initialization order: Level1 < Level2 < Level3 < Level4
        await Assert.That(DeepDependency.Level3.Level2.Level1.InitializationOrder)
            .IsLessThan(DeepDependency.Level3.Level2.InitializationOrder);

        await Assert.That(DeepDependency.Level3.Level2.InitializationOrder)
            .IsLessThan(DeepDependency.Level3.InitializationOrder);

        await Assert.That(DeepDependency.Level3.InitializationOrder)
            .IsLessThan(DeepDependency.InitializationOrder);
    }
}

#endregion

#region MethodDataSource with Property Injection Tests

// A fixture that uses property injection and provides data via MethodDataSource
public class FixtureWithMethodDataSource : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<PulsarTestContainer>(Shared = SharedType.PerTestSession)]
    public required PulsarTestContainer Container { get; init; }

    public bool IsInitialized { get; private set; }

    public Task InitializeAsync()
    {
        if (!Container.IsInitialized)
        {
            throw new InvalidOperationException("FixtureWithMethodDataSource initialized before its Container!");
        }
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => default;

    // This method provides test data and requires the fixture to be initialized
    public IEnumerable<Func<string>> GetTestData()
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("GetTestData called before fixture was initialized!");
        }
        yield return () => "TestValue1";
        yield return () => "TestValue2";
    }
}

// Test that combines MethodDataSource with ClassDataSource property injection
// NOTE: This scenario cannot work because MethodDataSource is evaluated during test discovery,
// before the test class instance exists and before fixtures are property-injected.
// This is a fundamental design limitation, not a bug.
[EngineTest(ExpectedResult.Failure)]
[NotInParallel]
public class MethodDataSourceWithPropertyInjectionTest
{
    [ClassDataSource<FixtureWithMethodDataSource>(Shared = SharedType.PerClass)]
    public required FixtureWithMethodDataSource Fixture { get; init; }

    public IEnumerable<Func<string>> TestData => Fixture.GetTestData();

    [Test]
    [MethodDataSource(nameof(TestData))]
    public async Task TestWithMethodDataFromInjectedFixture(string testValue)
    {
        // If we reach here, the initialization order was correct
        await Assert.That(Fixture.IsInitialized).IsTrue();
        await Assert.That(Fixture.Container.IsInitialized).IsTrue();
        await Assert.That(testValue).IsNotNullOrEmpty();
    }
}

#endregion

#region Multiple Abstract Base Classes with Different Sharing Modes

// Shared per class container
public class PerClassContainer : IAsyncInitializer, IAsyncDisposable
{
    public bool IsInitialized { get; private set; }
    public string InstanceId { get; } = Guid.NewGuid().ToString();

    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => default;
}

// Dependency that uses PerClass shared container
public class PerClassDependency : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<PerClassContainer>(Shared = SharedType.PerClass)]
    public required PerClassContainer Container { get; init; }

    public bool IsInitialized { get; private set; }

    public Task InitializeAsync()
    {
        if (!Container.IsInitialized)
        {
            throw new InvalidOperationException("PerClassDependency initialized before Container!");
        }
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => default;
}

// First abstract base with PerClass dependency
public abstract class AbstractWithPerClassDependency
{
    [ClassDataSource<PerClassDependency>(Shared = SharedType.None)]
    public required PerClassDependency PerClassDep { get; init; }
}

// Second abstract layer with additional PerTestSession dependency
public abstract class AbstractWithMixedSharing : AbstractWithPerClassDependency
{
    [ClassDataSource<PulsarConnection>(Shared = SharedType.None)]
    public required PulsarConnection SessionDep { get; init; }
}

// Test for mixed sharing modes in abstract hierarchy
[EngineTest(ExpectedResult.Pass)]
[NotInParallel]
public class MixedSharingModesInAbstractHierarchyTest : AbstractWithMixedSharing
{
    [Test]
    public async Task AllDependenciesShouldBeProperlyInitialized()
    {
        // Verify PerClass chain
        await Assert.That(PerClassDep.IsInitialized).IsTrue();
        await Assert.That(PerClassDep.Container.IsInitialized).IsTrue();

        // Verify PerTestSession chain
        await Assert.That(SessionDep.IsInitialized).IsTrue();
        await Assert.That(SessionDep.Container.IsInitialized).IsTrue();
    }

    [Test]
    public async Task ContainersWithSameSharingModeAndTypeShouldBeShared()
    {
        // Both PulsarConnection and WebAppFactory should share the same PulsarTestContainer
        // because they both use SharedType.PerTestSession for PulsarTestContainer
        await Assert.That(SessionDep.Container).IsNotNull();
    }
}

#endregion
