using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using TUnit.AspNetCore;
using TUnit.Core.Interfaces;

namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// Tests for issue #4431 comment: "API starting before ConfigureAppConfiguration"
/// https://github.com/thomhurst/TUnit/issues/4431#issuecomment-3769625721
///
/// The user reports that even with TUnit 1.12.3, the API starts before
/// ConfigureAppConfiguration receives properly initialized values from
/// ClassDataSource properties (like database containers).
///
/// This test verifies the execution order:
/// 1. ClassDataSource properties should be injected and initialized (IAsyncInitializer)
/// 2. THEN ConfigureWebHost/ConfigureAppConfiguration should be called
/// </summary>
public static class ExecutionOrderTracker
{
    private static int _orderCounter;

    public static int DataSourceInitializedAt { get; private set; }
    public static int ConfigureWebHostCalledAt { get; private set; }
    public static int ConfigureAppConfigurationCalledAt { get; private set; }
    public static string? ConnectionStringAtConfigureTime { get; private set; }
    public static bool WasDataSourceNullAtConfigureTime { get; private set; }

    public static void Reset()
    {
        _orderCounter = 0;
        DataSourceInitializedAt = 0;
        ConfigureWebHostCalledAt = 0;
        ConfigureAppConfigurationCalledAt = 0;
        ConnectionStringAtConfigureTime = null;
        WasDataSourceNullAtConfigureTime = false;
    }

    public static int GetNextOrder() => Interlocked.Increment(ref _orderCounter);

    public static void RecordDataSourceInitialized()
    {
        DataSourceInitializedAt = GetNextOrder();
        Console.WriteLine($"[ORDER {DataSourceInitializedAt}] DataSource.InitializeAsync completed");
    }

    public static void RecordConfigureWebHostCalled()
    {
        ConfigureWebHostCalledAt = GetNextOrder();
        Console.WriteLine($"[ORDER {ConfigureWebHostCalledAt}] ConfigureWebHost called");
    }

    public static void RecordConfigureAppConfigurationCalled(string? connectionString, bool wasNull)
    {
        ConfigureAppConfigurationCalledAt = GetNextOrder();
        ConnectionStringAtConfigureTime = connectionString;
        WasDataSourceNullAtConfigureTime = wasNull;
        Console.WriteLine($"[ORDER {ConfigureAppConfigurationCalledAt}] ConfigureAppConfiguration called, connectionString={connectionString}, wasNull={wasNull}");
    }
}

/// <summary>
/// Simulates a database container that requires async initialization.
/// This mimics Testcontainers behavior where the container must start
/// before a connection string is available.
/// </summary>
public class SimulatedDatabaseContainer : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }
    public string? ConnectionString { get; private set; }

    public async Task InitializeAsync()
    {
        Console.WriteLine("SimulatedDatabaseContainer.InitializeAsync STARTING");

        // Simulate container startup time
        await Task.Delay(50);

        IsInitialized = true;
        ConnectionString = "Host=container;Port=5432;Database=testdb;Username=test;Password=test";

        ExecutionOrderTracker.RecordDataSourceInitialized();
        Console.WriteLine("SimulatedDatabaseContainer.InitializeAsync COMPLETED");
    }
}

/// <summary>
/// Test factory that has a ClassDataSource property for the database container.
/// The issue is whether this property is injected BEFORE ConfigureWebHost is called.
/// </summary>
public class TestFactoryWithSimulatedDataSource : TestWebApplicationFactory<Program>
{
    [ClassDataSource<SimulatedDatabaseContainer>(Shared = SharedType.PerTestSession)]
    public SimulatedDatabaseContainer SimulatedDatabase { get; init; } = null!;

    protected override void ConfigureStartupConfiguration(IConfigurationBuilder configurationBuilder)
    {
        // This is required by the Program.cs validation
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "SomeKey", "SomeValue" }
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ExecutionOrderTracker.RecordConfigureWebHostCalled();

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            // This is the critical moment - is SimulatedDatabase initialized yet?
            var wasNull = SimulatedDatabase == null;
            string? connectionString = null;

            try
            {
                connectionString = SimulatedDatabase?.ConnectionString;
            }
            catch (NullReferenceException)
            {
                wasNull = true;
            }

            ExecutionOrderTracker.RecordConfigureAppConfigurationCalled(connectionString, wasNull);

            // Add required configuration for the app
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:ConnectionString", connectionString ?? "fallback" },
                { "Database:TableName", "test_todos" },
                { "Redis:ConnectionString", "localhost:6379" },
                { "Kafka:ConnectionString", "localhost:9092" }
            });
        });
    }
}

/// <summary>
/// Test class that uses the factory with simulated data source.
/// This test verifies whether the data source is initialized BEFORE ConfigureWebHost is called.
///
/// IMPORTANT: The tracker captures execution order DURING GlobalFactory initialization,
/// which happens before [Before(HookType.Test)] runs. We verify the captured state.
/// </summary>
[NotInParallel(nameof(ConfigureAppConfigurationTimingTests), Order = 1)]
public class ConfigureAppConfigurationTimingTests : WebApplicationTest<TestFactoryWithSimulatedDataSource, Program>
{
    [Test]
    public async Task DataSource_Should_Be_Initialized_Before_ConfigureAppConfiguration()
    {
        // The GlobalFactory is already initialized at this point (it's PerTestSession).
        // The execution order was captured during that initialization.
        // Accessing the per-test Factory will trigger another ConfigureWebHost call.

        // First, verify the GlobalFactory's SimulatedDatabase is properly initialized
        await Assert.That(GlobalFactory.SimulatedDatabase).IsNotNull()
            .Because("GlobalFactory.SimulatedDatabase should be injected before ConfigureWebHost was called");

        await Assert.That(GlobalFactory.SimulatedDatabase.IsInitialized).IsTrue()
            .Because("SimulatedDatabase should be initialized via IAsyncInitializer");

        await Assert.That(GlobalFactory.SimulatedDatabase.ConnectionString)
            .IsNotNull()
            .And.Contains("container")
            .Because("Connection string should be from the initialized container");

        // Access the factory to trigger creation of the per-test isolated factory
        _ = Factory.Server;

        // Log the execution order that was captured during initialization
        Console.WriteLine($"DataSource initialized at order: {ExecutionOrderTracker.DataSourceInitializedAt}");
        Console.WriteLine($"ConfigureWebHost called at order: {ExecutionOrderTracker.ConfigureWebHostCalledAt}");
        Console.WriteLine($"ConfigureAppConfiguration called at order: {ExecutionOrderTracker.ConfigureAppConfigurationCalledAt}");
        Console.WriteLine($"Connection string at configure time: {ExecutionOrderTracker.ConnectionStringAtConfigureTime}");
        Console.WriteLine($"Was DataSource null at configure time: {ExecutionOrderTracker.WasDataSourceNullAtConfigureTime}");

        // The key assertion: DataSource should NOT have been null when ConfigureAppConfiguration ran
        await Assert.That(ExecutionOrderTracker.WasDataSourceNullAtConfigureTime)
            .IsFalse()
            .Because("SimulatedDatabase property should not be null when ConfigureAppConfiguration runs - " +
                     "this would indicate the API started before the data source was initialized");

        // The connection string should have been available (not null and not "fallback")
        await Assert.That(ExecutionOrderTracker.ConnectionStringAtConfigureTime)
            .IsNotNull()
            .And.Contains("container")
            .Because("Connection string should be from the initialized container, not a fallback value");
    }

    [Test]
    public async Task GlobalFactory_SimulatedDatabase_Should_Be_Initialized()
    {
        // Verify that the GlobalFactory's SimulatedDatabase property is properly initialized
        await Assert.That(GlobalFactory.SimulatedDatabase).IsNotNull()
            .Because("GlobalFactory.SimulatedDatabase should be injected");

        await Assert.That(GlobalFactory.SimulatedDatabase.IsInitialized).IsTrue()
            .Because("SimulatedDatabase should be initialized via IAsyncInitializer");

        await Assert.That(GlobalFactory.SimulatedDatabase.ConnectionString)
            .IsNotNull()
            .And.Contains("container")
            .Because("Connection string should be available after initialization");
    }
}
