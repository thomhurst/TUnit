using System.Collections.Concurrent;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs.ComplexInfrastructureOrchestration;

/// <summary>
/// Regression test for complex test infrastructure orchestration issue.
/// When a test class has an injected property (WebApplicationFactory) that itself
/// has injected properties (InMemoryDatabase), the initialization order must ensure
/// that nested dependencies are initialized BEFORE the parent object.
/// 
/// Before fix: Both InMemoryDatabase and WebApplicationFactory were at depth 0,
/// causing parallel initialization and access to uninitialized Container.
/// 
/// After fix: InMemoryDatabase is at depth 1, WebApplicationFactory at depth 0,
/// ensuring InMemoryDatabase.InitializeAsync completes before WebApplicationFactory.InitializeAsync starts.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class NestedIAsyncInitializerOrderTests
{
    private static readonly object InitializationOrderLock = new();
    private static readonly List<string> InitializationOrder = new();

    [ClassDataSource<MockWebApplicationFactory>(Shared = SharedType.None)]
    public required MockWebApplicationFactory AppFactory { get; init; }

    [Test]
    public async Task NestedDependencies_InitializeInCorrectOrder()
    {
        // Verify that the database was initialized before the app factory
        List<string> order;
        lock (InitializationOrderLock)
        {
            order = new List<string>(InitializationOrder);
        }
        
        // Should have exactly 2 initialization events
        await Assert.That(order.Count).IsEqualTo(2);
        
        // Database should be initialized first (index 0)
        await Assert.That(order[0]).IsEqualTo("Database.InitializeAsync");
        
        // App factory should be initialized second (index 1)
        await Assert.That(order[1]).IsEqualTo("AppFactory.InitializeAsync");
        
        // Verify that the connection string was successfully accessed
        await Assert.That(AppFactory.ConnectionString).IsNotNull();
        await Assert.That(AppFactory.ConnectionString).Contains("Server=localhost;Port=5432");
    }

    [After(Test)]
    public void ResetState()
    {
        lock (InitializationOrderLock)
        {
            InitializationOrder.Clear();
        }
    }

    /// <summary>
    /// Simulates a database container that must be started before use.
    /// </summary>
    public class MockInMemoryDatabase : IAsyncInitializer, IAsyncDisposable
    {
        private bool _isStarted;
        
        public string ConnectionString { get; private set; } = string.Empty;

        public Task InitializeAsync()
        {
            lock (InitializationOrderLock)
            {
                InitializationOrder.Add("Database.InitializeAsync");
            }
            
            // Simulate starting the container
            _isStarted = true;
            ConnectionString = "Server=localhost;Port=5432;Database=test;";
            
            return Task.CompletedTask;
        }

        public string GetConnectionString()
        {
            if (!_isStarted)
            {
                throw new InvalidOperationException("Container not started! InitializeAsync must be called before accessing GetConnectionString.");
            }
            
            return ConnectionString;
        }

        public ValueTask DisposeAsync()
        {
            _isStarted = false;
            return default;
        }
    }

    /// <summary>
    /// Simulates a WebApplicationFactory that depends on the database being initialized.
    /// The ConfigureWebHost equivalent accesses the database connection string.
    /// </summary>
    public class MockWebApplicationFactory : IAsyncInitializer, IAsyncDisposable
    {
        [ClassDataSource<MockInMemoryDatabase>(Shared = SharedType.None)]
        public required MockInMemoryDatabase Database { get; init; }

        public string? ConnectionString { get; private set; }

        public Task InitializeAsync()
        {
            lock (InitializationOrderLock)
            {
                InitializationOrder.Add("AppFactory.InitializeAsync");
            }
            
            // This simulates accessing Server property which triggers ConfigureWebHost
            // ConfigureWebHost needs to access Database.GetConnectionString()
            // This will throw if Database.InitializeAsync hasn't completed yet
            ConnectionString = ConfigureWebHost();
            
            return Task.CompletedTask;
        }

        private string ConfigureWebHost()
        {
            // This is the critical access that was failing before the fix
            // Database.GetConnectionString() requires that Database.InitializeAsync() has completed
            return Database.GetConnectionString();
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }
    }
}
