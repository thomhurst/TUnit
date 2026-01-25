using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._4032;

/// <summary>
/// Regression tests for issue #4032: IAsyncInitializer lifecycle change broke complex test infrastructure orchestration.
/// The problem was that nested IAsyncInitializer dependencies weren't initialized before the parent's InitializeAsync was called.
/// This broke patterns like WebApplicationFactory + Testcontainers where the container must be initialized
/// before the WebApplicationFactory tries to access the container's connection string.
/// </summary>

#region Mock Classes

/// <summary>
/// Simulates a Testcontainers PostgreSQL container.
/// Must be initialized before any parent object tries to access GetConnectionString().
/// </summary>
public class MockDatabaseContainer : IAsyncInitializer
{
    private static int _instanceCount;
    private static int _initializeCount;
    private static readonly List<int> _initializationOrder = [];

    public static int InstanceCount => _instanceCount;
    public static int InitializeCount => _initializeCount;
    public static IReadOnlyList<int> InitializationOrder => _initializationOrder;

    public int InstanceId { get; }
    public bool IsInitialized { get; private set; }
    public string? ConnectionString { get; private set; }

    public MockDatabaseContainer()
    {
        InstanceId = Interlocked.Increment(ref _instanceCount);
    }

    public Task InitializeAsync()
    {
        Interlocked.Increment(ref _initializeCount);
        IsInitialized = true;
        ConnectionString = $"Host=localhost;Database=test_{InstanceId};User=test;Password=test";

        lock (_initializationOrder)
        {
            _initializationOrder.Add(InstanceId);
        }

        Console.WriteLine($"[MockDatabaseContainer #{InstanceId}] InitializeAsync called - ConnectionString: {ConnectionString}");
        return Task.CompletedTask;
    }

    public string GetConnectionString()
    {
        if (!IsInitialized || ConnectionString == null)
        {
            throw new InvalidOperationException(
                $"MockDatabaseContainer #{InstanceId} is not initialized! " +
                "This indicates a bug in the IAsyncInitializer lifecycle ordering. " +
                "Nested dependencies must be initialized before parent objects.");
        }

        return ConnectionString;
    }

    public static void Reset()
    {
        _instanceCount = 0;
        _initializeCount = 0;
        lock (_initializationOrder)
        {
            _initializationOrder.Clear();
        }
    }
}

/// <summary>
/// Simulates a WebApplicationFactory that depends on a database container.
/// The container MUST be initialized before this class's InitializeAsync is called,
/// because InitializeAsync accesses the container's connection string.
/// </summary>
public class MockWebApplicationFactory : IAsyncInitializer
{
    private static int _instanceCount;
    private static int _initializeCount;

    public static int InstanceCount => _instanceCount;
    public static int InitializeCount => _initializeCount;

    public int InstanceId { get; }
    public bool IsInitialized { get; private set; }
    public string? ConfiguredConnectionString { get; private set; }

    [ClassDataSource<MockDatabaseContainer>(Shared = SharedType.PerTestSession)]
    public required MockDatabaseContainer Database { get; init; }

    public MockWebApplicationFactory()
    {
        InstanceId = Interlocked.Increment(ref _instanceCount);
    }

    public Task InitializeAsync()
    {
        Interlocked.Increment(ref _initializeCount);

        // This is the critical part: we need to access the container's connection string
        // during initialization. If the container isn't initialized yet, this will throw.
        ConfiguredConnectionString = Database.GetConnectionString();
        IsInitialized = true;

        Console.WriteLine($"[MockWebApplicationFactory #{InstanceId}] InitializeAsync called - Using connection: {ConfiguredConnectionString}");
        return Task.CompletedTask;
    }

    public static void Reset()
    {
        _instanceCount = 0;
        _initializeCount = 0;
    }
}

#endregion

#region Tests

/// <summary>
/// Tests that nested IAsyncInitializer dependencies are initialized before the parent's InitializeAsync.
/// This is the exact pattern from issue #4032.
/// </summary>
[NotInParallel]
[EngineTest(ExpectedResult.Pass)]
public class NestedAsyncInitializerOrderTests
{
    [ClassDataSource<MockWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required MockWebApplicationFactory Factory { get; init; }

    [Before(Class)]
    public static void ResetCounters()
    {
        MockDatabaseContainer.Reset();
        MockWebApplicationFactory.Reset();
    }

    [Test]
    public async Task Factory_ShouldBeInitialized()
    {
        // The factory should be initialized (which means the database must have been initialized first)
        await Assert.That(Factory.IsInitialized).IsTrue()
            .Because("the factory's InitializeAsync should have been called");
    }

    [Test]
    public async Task Factory_ShouldHaveConfiguredConnectionString()
    {
        // The factory should have captured the connection string during initialization
        await Assert.That(Factory.ConfiguredConnectionString).IsNotNull()
            .Because("the factory should have accessed the database connection string during InitializeAsync");
    }

    [Test]
    public async Task Database_ShouldBeInitializedBeforeFactory()
    {
        // The database should be initialized
        await Assert.That(Factory.Database.IsInitialized).IsTrue()
            .Because("the database container should be initialized before the factory accesses it");
    }

    [Test]
    public async Task Database_InitializeCount_ShouldBeOne()
    {
        // The database should only be initialized once (shared per test session)
        await Assert.That(MockDatabaseContainer.InitializeCount).IsEqualTo(1)
            .Because("the database container is shared PerTestSession and should only be initialized once");
    }

    [Test]
    public async Task Factory_InitializeCount_ShouldBeOne()
    {
        // The factory should only be initialized once (shared per test session)
        await Assert.That(MockWebApplicationFactory.InitializeCount).IsEqualTo(1)
            .Because("the factory is shared PerTestSession and should only be initialized once");
    }
}

/// <summary>
/// Tests deep nesting (3 levels) of IAsyncInitializer dependencies.
/// Uses a shared static list to track initialization order reliably.
/// </summary>
public static class DeepNestingInitializationTracker
{
    private static readonly List<string> _initializationOrder = [];
    private static readonly Lock _lock = new();

    public static void RecordInitialization(string name)
    {
        lock (_lock)
        {
            _initializationOrder.Add(name);
        }
    }

    public static IReadOnlyList<string> GetOrder()
    {
        lock (_lock)
        {
            return _initializationOrder.ToList();
        }
    }

    public static void Reset()
    {
        lock (_lock)
        {
            _initializationOrder.Clear();
        }
    }
}

public class DeepNestedDependency : IAsyncInitializer
{
    private static int _instanceCount;
    private static int _initializeCount;

    public static int InstanceCount => _instanceCount;
    public static int InitializeCount => _initializeCount;

    public int InstanceId { get; }
    public bool IsInitialized { get; private set; }
    public string? Value { get; private set; }

    public DeepNestedDependency()
    {
        InstanceId = Interlocked.Increment(ref _instanceCount);
    }

    public Task InitializeAsync()
    {
        Interlocked.Increment(ref _initializeCount);
        IsInitialized = true;
        Value = $"DeepValue_{InstanceId}";
        DeepNestingInitializationTracker.RecordInitialization("Deep");
        Console.WriteLine($"[DeepNestedDependency #{InstanceId}] InitializeAsync called");
        return Task.CompletedTask;
    }

    public string GetValue()
    {
        if (!IsInitialized || Value == null)
        {
            throw new InvalidOperationException($"DeepNestedDependency #{InstanceId} is not initialized!");
        }

        return Value;
    }

    public static void Reset()
    {
        _instanceCount = 0;
        _initializeCount = 0;
    }
}

public class MiddleDependency : IAsyncInitializer
{
    private static int _instanceCount;
    private static int _initializeCount;

    public static int InstanceCount => _instanceCount;
    public static int InitializeCount => _initializeCount;

    public int InstanceId { get; }
    public bool IsInitialized { get; private set; }
    public string? CombinedValue { get; private set; }

    [ClassDataSource<DeepNestedDependency>(Shared = SharedType.PerTestSession)]
    public required DeepNestedDependency DeepDependency { get; init; }

    public MiddleDependency()
    {
        InstanceId = Interlocked.Increment(ref _instanceCount);
    }

    public Task InitializeAsync()
    {
        Interlocked.Increment(ref _initializeCount);

        // Access the deep dependency - must be initialized first
        CombinedValue = $"Middle_{InstanceId}_{DeepDependency.GetValue()}";
        IsInitialized = true;
        DeepNestingInitializationTracker.RecordInitialization("Middle");
        Console.WriteLine($"[MiddleDependency #{InstanceId}] InitializeAsync called - CombinedValue: {CombinedValue}");
        return Task.CompletedTask;
    }

    public string GetCombinedValue()
    {
        if (!IsInitialized || CombinedValue == null)
        {
            throw new InvalidOperationException($"MiddleDependency #{InstanceId} is not initialized!");
        }

        return CombinedValue;
    }

    public static void Reset()
    {
        _instanceCount = 0;
        _initializeCount = 0;
    }
}

public class TopLevelDependency : IAsyncInitializer
{
    private static int _instanceCount;
    private static int _initializeCount;

    public static int InstanceCount => _instanceCount;
    public static int InitializeCount => _initializeCount;

    public int InstanceId { get; }
    public bool IsInitialized { get; private set; }
    public string? FinalValue { get; private set; }

    [ClassDataSource<MiddleDependency>(Shared = SharedType.PerTestSession)]
    public required MiddleDependency MiddleDependency { get; init; }

    public TopLevelDependency()
    {
        InstanceId = Interlocked.Increment(ref _instanceCount);
    }

    public Task InitializeAsync()
    {
        Interlocked.Increment(ref _initializeCount);

        // Access the middle dependency - must be initialized first
        FinalValue = $"Top_{InstanceId}_{MiddleDependency.GetCombinedValue()}";
        IsInitialized = true;
        DeepNestingInitializationTracker.RecordInitialization("Top");
        Console.WriteLine($"[TopLevelDependency #{InstanceId}] InitializeAsync called - FinalValue: {FinalValue}");
        return Task.CompletedTask;
    }

    public static void Reset()
    {
        _instanceCount = 0;
        _initializeCount = 0;
    }
}

/// <summary>
/// Tests 3-level deep nesting of IAsyncInitializer dependencies.
/// Order must be: DeepNestedDependency -> MiddleDependency -> TopLevelDependency
/// </summary>
[NotInParallel]
[EngineTest(ExpectedResult.Pass)]
public class DeepNestedAsyncInitializerOrderTests
{
    [ClassDataSource<TopLevelDependency>(Shared = SharedType.PerTestSession)]
    public required TopLevelDependency TopLevel { get; init; }

    [Before(Class)]
    public static void ResetCounters()
    {
        DeepNestedDependency.Reset();
        MiddleDependency.Reset();
        TopLevelDependency.Reset();
        DeepNestingInitializationTracker.Reset();
    }

    [Test]
    public async Task AllLevels_ShouldBeInitialized()
    {
        await Assert.That(TopLevel.IsInitialized).IsTrue();
        await Assert.That(TopLevel.MiddleDependency.IsInitialized).IsTrue();
        await Assert.That(TopLevel.MiddleDependency.DeepDependency.IsInitialized).IsTrue();
    }

    [Test]
    public async Task InitializationOrder_ShouldBeDeepestFirst()
    {
        // Get the recorded initialization order
        var order = DeepNestingInitializationTracker.GetOrder();

        // Deep should be initialized first, then Middle, then Top
        await Assert.That(order).HasCount().EqualTo(3)
            .Because("there should be exactly 3 initializations");

        await Assert.That(order[0]).IsEqualTo("Deep")
            .Because("the deepest dependency should be initialized first");

        await Assert.That(order[1]).IsEqualTo("Middle")
            .Because("the middle dependency should be initialized second");

        await Assert.That(order[2]).IsEqualTo("Top")
            .Because("the top-level dependency should be initialized last");
    }

    [Test]
    public async Task FinalValue_ShouldContainAllLevels()
    {
        // The final value should contain data from all three levels
        await Assert.That(TopLevel.FinalValue).Contains("Top_");
        await Assert.That(TopLevel.FinalValue).Contains("Middle_");
        await Assert.That(TopLevel.FinalValue).Contains("DeepValue_");
    }

    [Test]
    public async Task EachLevel_ShouldBeInitializedOnce()
    {
        await Assert.That(DeepNestedDependency.InitializeCount).IsEqualTo(1);
        await Assert.That(MiddleDependency.InitializeCount).IsEqualTo(1);
        await Assert.That(TopLevelDependency.InitializeCount).IsEqualTo(1);
    }
}

#endregion
