# Property Injection

TUnit's AOT-compatible property injection system makes it easy to initialize properties on your test class with compile-time safety and excellent performance.

Your properties must be marked with the `required` keyword and then simply place a data attribute on it.
The required keyword keeps your code clean and correct. If a property isn't passed in, you'll get a compiler warning, so you know something has gone wrong. It also gets rid of any pesky nullability warnings.

## AOT-Compatible Property Attributes

Supported attributes for properties in AOT mode:
- **MethodDataSource** - Data sources via calling a method
- **`ClassDataSource<T>`** - A data source that injects in T
- **DataSourceGeneratorAttribute** - Source-generated data (first item only)

For dependency injection with service providers, inherit from `DependencyInjectionDataSourceAttribute<TScope>` to create custom attributes. See [Dependency Injection](./dependency-injection.md) documentation for details.

The AOT system generates strongly-typed property setters at compile time, eliminating reflection overhead and ensuring Native AOT compatibility.

## Async Property Initialization

Properties can implement `IAsyncInitializer` for complex setup scenarios with automatic lifecycle management:

:::warning Discovery Phase vs Execution Phase
`IAsyncInitializer` runs during **test execution**, not during **test discovery**.

Test discovery happens when:
- Your IDE loads/reloads the project
- You run `dotnet run --list-tests`
- CI/CD systems enumerate tests before running them

During discovery, `IAsyncInitializer` has **not yet run**, so properties will be uninitialized. If you use `InstanceMethodDataSource` or similar features that access instance properties during discovery, you may get empty data or no tests generated.

**When you need discovery-time initialization**, use `IAsyncDiscoveryInitializer` instead (see [Discovery Phase Initialization](#discovery-phase-initialization) below).
:::

```csharp
using TUnit.Core;

namespace MyTestProject;

public class AsyncPropertyExample : IAsyncInitializer, IAsyncDisposable
{
    public bool IsInitialized { get; private set; }
    public string? ConnectionString { get; private set; }

    public async Task InitializeAsync()
    {
        await Task.Delay(10); // Simulate async setup
        ConnectionString = "Server=localhost;Database=test";
        IsInitialized = true;
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Delay(1); // Cleanup
        IsInitialized = false;
        ConnectionString = null;
    }
}
```

## Discovery Phase Initialization

### When Discovery Initialization is Needed

Most tests don't need discovery-time initialization. Discovery-time initialization is only required when:

1. You use `InstanceMethodDataSource` and the data source method returns **dynamically loaded data** (not predefined values)
2. The test case enumeration depends on async-loaded data (e.g., querying a database for test cases)

**Performance Note:** Discovery happens frequently (IDE reloads, project switches, `--list-tests`), so discovery-time initialization runs more often than test execution. Avoid expensive operations in `IAsyncDiscoveryInitializer` when possible.

### Using IAsyncDiscoveryInitializer

When you need data available during discovery, implement `IAsyncDiscoveryInitializer` instead of `IAsyncInitializer`:

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

<Tabs>
  <TabItem value="wrong" label="❌ Wrong - Data not available during discovery" default>

```csharp
// This fixture's data won't be available during discovery
public class TestDataFixture : IAsyncInitializer, IAsyncDisposable
{
    private List<string> _testCases = [];

    public async Task InitializeAsync()
    {
        // This runs during EXECUTION, not DISCOVERY
        _testCases = await LoadTestCasesFromDatabaseAsync();
    }

    // This will return empty list during discovery!
    public IEnumerable<string> GetTestCases() => _testCases;

    public async ValueTask DisposeAsync()
    {
        _testCases.Clear();
    }
}

public class MyTests
{
    [ClassDataSource<TestDataFixture>(Shared = SharedType.PerClass)]
    public required TestDataFixture Fixture { get; init; }

    // During discovery, Fixture.GetTestCases() returns empty list
    // Result: No tests are generated!
    public IEnumerable<string> TestCases => Fixture.GetTestCases();

    [Test]
    [InstanceMethodDataSource(nameof(TestCases))]
    public async Task MyTest(string testCase)
    {
        // This test is never created because TestCases was empty during discovery
        await Assert.That(testCase).IsNotNullOrEmpty();
    }
}
```

  </TabItem>
  <TabItem value="correct" label="✅ Correct - Uses IAsyncDiscoveryInitializer">

```csharp
// This fixture's data IS available during discovery
public class TestDataFixture : IAsyncDiscoveryInitializer, IAsyncDisposable
{
    private List<string> _testCases = [];

    public async Task InitializeAsync()
    {
        // This runs during DISCOVERY, before test enumeration
        _testCases = await LoadTestCasesFromDatabaseAsync();
    }

    // This returns populated list during discovery!
    public IEnumerable<string> GetTestCases() => _testCases;

    public async ValueTask DisposeAsync()
    {
        _testCases.Clear();
    }
}

public class MyTests
{
    [ClassDataSource<TestDataFixture>(Shared = SharedType.PerClass)]
    public required TestDataFixture Fixture { get; init; }

    // During discovery, Fixture is already initialized
    // Result: Tests are generated successfully!
    public IEnumerable<string> TestCases => Fixture.GetTestCases();

    [Test]
    [InstanceMethodDataSource(nameof(TestCases))]
    public async Task MyTest(string testCase)
    {
        // This test IS created with each test case from the fixture
        await Assert.That(testCase).IsNotNullOrEmpty();
    }
}
```

  </TabItem>
  <TabItem value="best" label="✅ Best - Predefined data without discovery initialization">

```csharp
// Best approach: Predefined test case IDs, expensive initialization during execution only
public class TestDataFixture : IAsyncInitializer, IAsyncDisposable
{
    // Test case IDs are predefined - no initialization needed for discovery
    private static readonly string[] PredefinedTestCases = ["Case1", "Case2", "Case3"];

    private DockerContainer? _container;

    public async Task InitializeAsync()
    {
        // Expensive initialization runs during EXECUTION only
        _container = await StartDockerContainerAsync();
    }

    // Returns predefined IDs - works during discovery without initialization
    public IEnumerable<string> GetTestCaseIds() => PredefinedTestCases;

    public async ValueTask DisposeAsync()
    {
        if (_container != null)
            await _container.DisposeAsync();
    }
}

public class MyTests
{
    [ClassDataSource<TestDataFixture>(Shared = SharedType.PerClass)]
    public required TestDataFixture Fixture { get; init; }

    // Returns predefined IDs - no initialization required during discovery
    public IEnumerable<string> TestCases => Fixture.GetTestCaseIds();

    [Test]
    [InstanceMethodDataSource(nameof(TestCases))]
    public async Task MyTest(string testCaseId)
    {
        // Fixture IS initialized by the time the test runs
        // Can now use the expensive resources (Docker container, etc.)
        await Assert.That(testCaseId).IsNotNullOrEmpty();
    }
}
```

  </TabItem>
</Tabs>

**Recommendation:** Prefer the "predefined data" approach when possible. This avoids expensive initialization during discovery, which happens frequently (IDE reloads, `--list-tests`, etc.).

For more troubleshooting, see [Test Discovery Issues](../troubleshooting.md#test-discovery-issues).

## Basic Property Injection Examples

```csharp
using TUnit.Core;

namespace MyTestProject;

public class PropertySetterTests
{
    // Static method data source injection
    [MethodDataSource(nameof(GetMethodData))]
    public required string Property2 { get; init; }

    // Class-based data source injection
    [ClassDataSource<InnerModel>]
    public required InnerModel Property3 { get; init; }

    // Globally shared data source
    [ClassDataSource<InnerModel>(Shared = SharedType.PerTestSession)]
    public required InnerModel Property4 { get; init; }

    // Class-scoped shared data source
    [ClassDataSource<InnerModel>(Shared = SharedType.PerClass)]
    public required InnerModel Property5 { get; init; }

    // Keyed shared data source
    [ClassDataSource<InnerModel>(Shared = SharedType.Keyed, Key = "Key")]
    public required InnerModel Property6 { get; init; }

    // Source-generated data injection
    [DataSourceGeneratorTests.AutoFixtureGenerator<string>]
    public required string Property7 { get; init; }

    // Async initialization example (IAsyncInitializer)
    [ClassDataSource<AsyncPropertyExample>]
    public required AsyncPropertyExample AsyncService { get; init; }

    [Test]
    public async Task Test()
    {
        // All properties are automatically initialized before this test runs
        await Assert.That(Property2).IsNotNull();
        await Assert.That(Property3).IsNotNull();
        await Assert.That(AsyncService.IsInitialized).IsTrue();

        Console.WriteLine($"Property7: {Property7}");
    }

    // Static data source method for Property2 - returns a single value for property injection
    public static string GetMethodData() => "method_data_value";
}

// Example model for ClassDataSource
public class InnerModel
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}
```

## Nested Property Injection

One of TUnit's most powerful features is nested property injection with automatic initialization. This allows you to inject objects into other objects created via data sources, enabling advanced test orchestration with relatively simple code. TUnit handles all the complex aspects like initialization order and object lifetimes.

### How It Works

When you use property injection with data source attributes, those injected objects can themselves have injected properties. TUnit will:
1. Resolve the entire dependency graph
2. Create objects in the correct order
3. Initialize them (if they implement `IAsyncInitializer`)
4. Inject them into parent objects
5. Dispose of them when appropriate (if they implement `IAsyncDisposable`)

### Example: Complex Test Infrastructure

Here's a comprehensive example showing how to orchestrate multiple test containers and a web application:

```csharp
// In-memory SQL container that auto-starts and stops
public class InMemorySql : IAsyncInitializer, IAsyncDisposable
{
    private TestcontainersContainer? _container;

    public TestcontainersContainer Container => _container
        ?? throw new InvalidOperationException("Container not initialized");

    public async Task InitializeAsync()
    {
        _container = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage("postgres:latest")
            .WithEnvironment("POSTGRES_PASSWORD", "password")
            .Build();

        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}

// Redis container with similar pattern
public class InMemoryRedis : IAsyncInitializer, IAsyncDisposable
{
    private TestcontainersContainer? _container;

    public TestcontainersContainer Container => _container
        ?? throw new InvalidOperationException("Container not initialized");

    public async Task InitializeAsync()
    {
        _container = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage("redis:latest")
            .Build();

        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}

// Message bus container
public class InMemoryMessageBus : IAsyncInitializer, IAsyncDisposable
{
    private TestcontainersContainer? _container;

    public TestcontainersContainer Container => _container
        ?? throw new InvalidOperationException("Container not initialized");

    public async Task InitializeAsync()
    {
        _container = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage("rabbitmq:3-management")
            .Build();

        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}

// UI component that depends on the message bus
public class MessageBusUserInterface : IAsyncInitializer, IAsyncDisposable
{
    private TestcontainersContainer? _container;

    // Inject the message bus dependency - shared per test session
    [ClassDataSource<InMemoryMessageBus>(Shared = SharedType.PerTestSession)]
    public required InMemoryMessageBus MessageBus { get; init; }

    public TestcontainersContainer Container => _container
        ?? throw new InvalidOperationException("Container not initialized");

    public async Task InitializeAsync()
    {
        // The MessageBus property is already initialized when this runs!
        _container = new MessageBusUIContainerBuilder()
            .WithConnectionString(MessageBus.Container.GetConnectionString())
            .Build();

        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}

// Web application factory that depends on multiple services
public class InMemoryWebApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    // Inject all required infrastructure - all shared per test session
    [ClassDataSource<InMemorySql>(Shared = SharedType.PerTestSession)]
    public required InMemorySql Sql { get; init; }

    [ClassDataSource<InMemoryRedis>(Shared = SharedType.PerTestSession)]
    public required InMemoryRedis Redis { get; init; }

    [ClassDataSource<InMemoryMessageBus>(Shared = SharedType.PerTestSession)]
    public required InMemoryMessageBus MessageBus { get; init; }

    public Task InitializeAsync()
    {
        // Force server creation to validate configuration
        _ = Server;
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            // All injected properties are already initialized!
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "MessageBus:ConnectionString", MessageBus.Container.GetConnectionString() },
                { "Redis:ConnectionString", Redis.Container.GetConnectionString() },
                { "PostgreSql:ConnectionString", Sql.Container.GetConnectionString() }
            });
        });
    }
}

// Your test class - clean and simple!
public class IntegrationTests
{
    // Just inject what you need - TUnit handles the entire dependency graph
    [ClassDataSource<InMemoryWebApplicationFactory>]
    public required InMemoryWebApplicationFactory WebApplicationFactory { get; init; }

    [ClassDataSource<MessageBusUserInterface>]
    public required MessageBusUserInterface MessageBusUI { get; init; }

    [Test]
    public async Task Full_Integration_Test()
    {
        // Everything is initialized in the correct order!
        var client = WebApplicationFactory.CreateClient();

        // Test your application with all infrastructure running
        var response = await client.GetAsync("/api/products");
        await Assert.That(response.IsSuccessStatusCode).IsTrue();

        // The MessageBusUI shares the same MessageBus instance as the WebApplicationFactory
        // because they both use SharedType.PerTestSession
    }
}
```

### Benefits of Nested Property Injection

1. **Simplified Test Setup**: You only need to declare what you need; TUnit handles the complex orchestration
2. **Automatic Lifecycle Management**: Objects are initialized in dependency order and disposed in reverse order
3. **Shared Resources**: Use `SharedType` to control object lifetime and reuse expensive resources
4. **Type Safety**: Everything is strongly typed with compile-time checking
5. **Clean Test Code**: Your tests focus on testing, not on infrastructure setup

### Sharing Strategies

When using nested property injection, the `Shared` parameter becomes crucial:

- **`SharedType.PerTestSession`**: Single instance for the entire test run - ideal for expensive resources like containers
- **`SharedType.PerAssembly`**: Single instance shared for every test in the same assembly as itself.
- **`SharedType.PerClass`**: One instance per test class
- **`SharedType.Keyed`**: Share instances based on a key value
- **`SharedType.None`**: New instance for each injection point (default)

### Best Practices

1. **Use Appropriate Sharing**: Share expensive resources like test containers using `PerTestSession`
2. **Implement IAsyncInitializer**: For complex setup that requires async operations
3. **Implement IAsyncDisposable**: Ensure proper cleanup of resources
4. **Order Independence**: Don't rely on initialization order between sibling properties
5. **Error Handling**: Initialization failures will fail the test with clear error messages

### Advanced Scenarios

#### Conditional Initialization

```csharp
public class ConditionalService : IAsyncInitializer
{
    [ClassDataSource<DatabaseService>(Shared = SharedType.PerTestSession)]
    public required DatabaseService Database { get; init; }

    public async Task InitializeAsync()
    {
        if (await Database.RequiresMigration())
        {
            await Database.MigrateAsync();
        }
    }
}
```

#### Circular Dependencies

TUnit will detect and report circular dependencies:

```csharp
public class ServiceA : IAsyncInitializer
{
    [ClassDataSource<ServiceB>]
    public required ServiceB B { get; init; } // This will fail!
}

public class ServiceB : IAsyncInitializer
{
    [ClassDataSource<ServiceA>]
    public required ServiceA A { get; init; } // Circular dependency!
}
```

This powerful feature makes complex test orchestration simple and maintainable, allowing you to focus on writing tests rather than managing test infrastructure!

