using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._4032;

/// <summary>
/// Regression test for issue #4032: IAsyncInitializer lifecycle change broke complex test infrastructure orchestration.
/// Verifies that nested IAsyncInitializer dependencies are initialized before their parent's InitializeAsync is called.
/// This mirrors the WebApplicationFactory + Testcontainers pattern where the container must be started
/// before the WebApplicationFactory can access its connection string.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class NestedAsyncInitializerTests
{
    [ClassDataSource<ParentWithNestedDependency>(Shared = SharedType.PerTestSession)]
    public required ParentWithNestedDependency Parent { get; init; }

    [Test]
    public async Task Parent_InitializeAsync_ShouldBeCalledAfterDependency()
    {
        // The parent's InitializeAsync should have been called after the child's
        await Assert.That(Parent.DependencyWasInitializedBeforeParent)
            .IsTrue()
            .Because("the nested dependency's InitializeAsync should be called before the parent's InitializeAsync");
    }

    [Test]
    public async Task Dependency_ShouldBeFullyInitialized_WhenParentAccessesIt()
    {
        // The dependency should be fully initialized when accessed by the parent
        await Assert.That(Parent.Dependency.IsInitialized)
            .IsTrue()
            .Because("the dependency should be initialized before the parent accesses it");
    }
}

/// <summary>
/// Test with constructor that accesses the nested dependency BEFORE InitializeAsync.
/// This simulates WebApplicationFactory's behavior where ConfigureWebHost is called during construction/access,
/// not during InitializeAsync.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class ConstructorAccessNestedInitializerTests
{
    [ClassDataSource<ParentAccessingDependencyInConstructor>(Shared = SharedType.PerTestSession)]
    public required ParentAccessingDependencyInConstructor Parent { get; init; }

    [Test]
    public async Task Dependency_ShouldBeInitialized_BeforeConstructorAccess()
    {
        // The dependency should have been initialized before the parent's constructor accessed it
        await Assert.That(Parent.DependencyWasInitializedAtConstruction)
            .IsTrue()
            .Because("the nested dependency's InitializeAsync should be called before the parent's constructor accesses it");
    }
}

/// <summary>
/// Test that matches the exact pattern from issue #4032:
/// - TestClass has WebApplicationFactory as a property
/// - WebApplicationFactory has InMemoryPostgreSqlDatabase as a property
/// - WebApplicationFactory.InitializeAsync() accesses Server which triggers ConfigureWebHost
/// - ConfigureWebHost accesses InMemoryPostgreSqlDatabase.Container.GetConnectionString()
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class WebApplicationFactoryPatternTests
{
    [ClassDataSource<MockWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required MockWebApplicationFactory WebAppFactory { get; init; }

    [Test]
    public async Task WebApplicationFactory_ShouldAccessInitializedContainer()
    {
        // The mock factory should have successfully accessed the container
        await Assert.That(WebAppFactory.ConnectionStringFromContainer)
            .IsNotNull()
            .Because("the container should be initialized before ConfigureWebHost is called");
    }

    [Test]
    public async Task Container_ShouldBeInitialized_BeforeFactoryInit()
    {
        await Assert.That(WebAppFactory.ContainerWasInitializedBeforeFactory)
            .IsTrue()
            .Because("InMemoryPostgreSqlDatabase.InitializeAsync should be called before WebApplicationFactory.InitializeAsync");
    }
}

/// <summary>
/// Simulates a container or resource that needs async initialization (like Testcontainers).
/// </summary>
public class NestedDependency : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }
    public DateTime InitializedAt { get; private set; }

    public Task InitializeAsync()
    {
        IsInitialized = true;
        InitializedAt = DateTime.UtcNow;
        Console.WriteLine($"[NestedDependency] InitializeAsync called at {InitializedAt:HH:mm:ss.fff}");
        return Task.CompletedTask;
    }

    public string GetConnectionString()
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("NestedDependency has not been initialized. Call InitializeAsync first.");
        }
        return "mock-connection-string";
    }
}

/// <summary>
/// Simulates InMemoryPostgreSqlDatabase from the issue.
/// </summary>
public class MockDatabase : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }

    public Task InitializeAsync()
    {
        IsInitialized = true;
        Console.WriteLine("[MockDatabase] Container started (InitializeAsync called)");
        return Task.CompletedTask;
    }

    public string GetConnectionString()
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("MockDatabase has not been initialized. Container not started.");
        }
        return "Host=localhost;Port=5432;Database=test;Username=test;Password=test";
    }
}

/// <summary>
/// Simulates WebApplicationFactory from the issue.
/// Accessing Server triggers ConfigureWebHost which needs the database.
/// </summary>
public class MockWebApplicationFactory : IAsyncInitializer
{
    [ClassDataSource<MockDatabase>(Shared = SharedType.PerTestSession)]
    public required MockDatabase Database { get; init; }

    public bool ContainerWasInitializedBeforeFactory { get; private set; }
    public string? ConnectionStringFromContainer { get; private set; }
    private bool _serverCreated;

    // Simulates WebApplicationFactory.Server property
    private void EnsureServer()
    {
        if (_serverCreated)
        {
            return;
        }

        // This simulates ConfigureWebHost being called
        ConfigureWebHost();
        _serverCreated = true;
    }

    // Simulates the ConfigureWebHost method from the issue
    private void ConfigureWebHost()
    {
        Console.WriteLine("[MockWebApplicationFactory] ConfigureWebHost called");
        ContainerWasInitializedBeforeFactory = Database.IsInitialized;

        // This is what fails in the issue - accessing the connection string
        // before the container is started
        ConnectionStringFromContainer = Database.GetConnectionString();
        Console.WriteLine($"[MockWebApplicationFactory] Got connection string: {ConnectionStringFromContainer}");
    }

    public Task InitializeAsync()
    {
        Console.WriteLine("[MockWebApplicationFactory] InitializeAsync called");
        // This triggers Server property which triggers ConfigureWebHost
        EnsureServer();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Simulates a parent class (like WebApplicationFactory) that depends on a nested resource.
/// The parent's InitializeAsync must be called AFTER the dependency's InitializeAsync.
/// </summary>
public class ParentWithNestedDependency : IAsyncInitializer
{
    [ClassDataSource<NestedDependency>(Shared = SharedType.PerTestSession)]
    public required NestedDependency Dependency { get; init; }

    public bool IsInitialized { get; private set; }
    public bool DependencyWasInitializedBeforeParent { get; private set; }
    public DateTime InitializedAt { get; private set; }
    public string? ConnectionStringFromDependency { get; private set; }

    public Task InitializeAsync()
    {
        InitializedAt = DateTime.UtcNow;
        Console.WriteLine($"[ParentWithNestedDependency] InitializeAsync called at {InitializedAt:HH:mm:ss.fff}");

        // Check if the dependency was already initialized
        DependencyWasInitializedBeforeParent = Dependency.IsInitialized;

        // This simulates accessing the container's connection string in ConfigureWebHost
        // If the dependency isn't initialized, this will throw
        ConnectionStringFromDependency = Dependency.GetConnectionString();

        IsInitialized = true;
        Console.WriteLine($"[ParentWithNestedDependency] DependencyWasInitializedBeforeParent: {DependencyWasInitializedBeforeParent}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Simulates a parent class (like WebApplicationFactory) that accesses the dependency DURING CONSTRUCTION.
/// This more accurately represents how WebApplicationFactory.Server property works - accessing it
/// triggers ConfigureWebHost before InitializeAsync is ever called.
/// </summary>
public class ParentAccessingDependencyInConstructor : IAsyncInitializer
{
    [ClassDataSource<NestedDependency>(Shared = SharedType.PerTestSession)]
    public required NestedDependency Dependency { get; init; }

    // This field is set when the "Server" property is accessed (simulating WebApplicationFactory.Server)
    private string? _serverConfig;
    private bool _dependencyWasInitializedAtConstruction;

    public bool DependencyWasInitializedAtConstruction => _dependencyWasInitializedAtConstruction;

    // Simulates WebApplicationFactory.Server which triggers ConfigureWebHost
    public string Server
    {
        get
        {
            if (_serverConfig == null)
            {
                // This simulates ConfigureWebHost being called when Server is first accessed
                // The dependency MUST be initialized by this point
                _dependencyWasInitializedAtConstruction = Dependency.IsInitialized;
                _serverConfig = Dependency.GetConnectionString();
            }
            return _serverConfig;
        }
    }

    public Task InitializeAsync()
    {
        Console.WriteLine($"[ParentAccessingDependencyInConstructor] InitializeAsync called");
        // Access Server to trigger "ConfigureWebHost"
        _ = Server;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Tests the case with multiple levels of nesting (3 levels deep).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class DeepNestedAsyncInitializerTests
{
    [ClassDataSource<Level0Parent>(Shared = SharedType.PerTestSession)]
    public required Level0Parent Root { get; init; }

    [Test]
    public async Task AllLevels_ShouldBeInitializedInCorrectOrder()
    {
        // Level 2 should be initialized first
        await Assert.That(Root.Level1.Level2.IsInitialized)
            .IsTrue()
            .Because("Level2 should be initialized");

        // Level 1 should be initialized after Level 2
        await Assert.That(Root.Level1.Level2WasInitializedFirst)
            .IsTrue()
            .Because("Level2 should be initialized before Level1");

        // Level 0 (Root) should be initialized after Level 1
        await Assert.That(Root.Level1WasInitializedFirst)
            .IsTrue()
            .Because("Level1 should be initialized before Level0 (Root)");
    }
}

public class Level2Dependency : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }

    public Task InitializeAsync()
    {
        IsInitialized = true;
        Console.WriteLine("[Level2] InitializeAsync called");
        return Task.CompletedTask;
    }
}

public class Level1Parent : IAsyncInitializer
{
    [ClassDataSource<Level2Dependency>(Shared = SharedType.PerTestSession)]
    public required Level2Dependency Level2 { get; init; }

    public bool IsInitialized { get; private set; }
    public bool Level2WasInitializedFirst { get; private set; }

    public Task InitializeAsync()
    {
        Level2WasInitializedFirst = Level2.IsInitialized;
        IsInitialized = true;
        Console.WriteLine($"[Level1] InitializeAsync called. Level2WasInitializedFirst: {Level2WasInitializedFirst}");
        return Task.CompletedTask;
    }
}

public class Level0Parent : IAsyncInitializer
{
    [ClassDataSource<Level1Parent>(Shared = SharedType.PerTestSession)]
    public required Level1Parent Level1 { get; init; }

    public bool IsInitialized { get; private set; }
    public bool Level1WasInitializedFirst { get; private set; }

    public Task InitializeAsync()
    {
        Level1WasInitializedFirst = Level1.IsInitialized;
        IsInitialized = true;
        Console.WriteLine($"[Level0] InitializeAsync called. Level1WasInitializedFirst: {Level1WasInitializedFirst}");
        return Task.CompletedTask;
    }
}
