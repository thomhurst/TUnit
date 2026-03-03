# Test Context

All tests have a `TestContext` object available to them.

This can be accessed statically via `TestContext.Current`.

Access information about the test, including things like the test name, containing class, custom properties, categories, etc.

Useful for a generic `AfterEachTest` for all tests, but with logic to execute for only certain tests.

```csharp
if (TestContext.Current?.Metadata.TestDetails.CustomProperties.ContainsKey("SomeProperty") == true)
{
    // Do something
}
```

The context has a `Results` object (nullable). It's null until an `AfterEachTest` method runs, since results are only available after test completion.

Use results to conditionally execute cleanup logic:

```csharp
if (TestContext.Current?.Result?.State == TestState.Failed)
{
    // Take a screenshot?
}
```

## Test Output and Artifacts

The `TestContext` provides multiple ways to write output and attach artifacts:

```csharp
// Write to standard output (modern interface-based approach)
TestContext.Current!.Output.WriteLine("Debug information");

// Alternative: Direct TextWriter access (also valid)
TestContext.Current!.OutputWriter.WriteLine("Debug information");

// Write to error output
TestContext.Current.Output.WriteError("Warning: something unexpected happened");

// Attach an artifact (file, screenshot, log, etc.)
TestContext.Current.Output.AttachArtifact(new Artifact
{
    File = new FileInfo("path/to/logfile.log"),
    DisplayName = "Application Logs",
    Description = "Logs captured during test execution"
});
```

Both `Output.WriteLine()` and `OutputWriter.WriteLine()` are valid - the `Output` property provides a convenient interface-based API, while `OutputWriter` gives direct access to the underlying TextWriter.

Artifacts are particularly useful for debugging test failures, especially in integration tests. You can attach screenshots, logs, videos, configuration files, or any other files that help diagnose issues.

For complete information about working with test artifacts, including session-level artifacts, best practices, and common use cases, see the [Test Artifacts](./artifacts.md) guide.

## Test Isolation

The `TestContext` provides built-in helpers for creating isolated resource names, ensuring parallel tests don't interfere with each other. Access them via `TestContext.Current!.Isolation`:

```csharp
// Get a unique ID for this test instance
var id = TestContext.Current!.Isolation.UniqueId;  // e.g. 42

// Create isolated resource names
var tableName = TestContext.Current!.Isolation.GetIsolatedName("todos");  // "Test_42_todos"
var topicName = TestContext.Current!.Isolation.GetIsolatedName("orders"); // "Test_42_orders"

// Create isolated key prefixes
var prefix = TestContext.Current!.Isolation.GetIsolatedPrefix();       // "test_42_"
var dotPrefix = TestContext.Current!.Isolation.GetIsolatedPrefix("."); // "test.42."
```

These are useful for any test that needs unique resource names — database tables, message queue topics, cache keys, blob storage paths, etc. — without requiring a specific base class.

:::tip ASP.NET Core Tests
If you're using `TUnit.AspNetCore`, the `WebApplicationTest` base class provides the same helpers as `protected` methods (`GetIsolatedName`, `GetIsolatedPrefix`). Both share the same underlying counter, so IDs are unique across all test types.
:::

## Custom Properties

Custom properties can be added to a test using the `[Property]` attribute. Properties are key-value pairs of strings that serve multiple purposes:

- **Test filtering**: Filter tests at the command line with `dotnet run --treenode-filter /*/*/*/*[PropertyName=PropertyValue]`
- **Runtime logic**: Access properties in setup/cleanup hooks via `TestContext` to conditionally execute logic
- **Inheritance**: Apply `[Property]` on a base class and all sub-class tests inherit it

```csharp
public class MyTestClass
{
    [Test]
    [Property("Category", "Integration")]
    public async Task MyTest(CancellationToken cancellationToken)
    {
        // Access the property at runtime
        var properties = TestContext.Current!.Metadata.TestDetails.CustomProperties;
        if (properties.ContainsKey("Category"))
        {
            // Conditional logic based on property
        }
    }
}
```

## Dependency Injection

**Note**: `TestContext` does NOT provide direct access to dependency injection services. The internal service provider in `TestContext` is exclusively for TUnit framework services and is not meant for user-provided dependencies.

If you need dependency injection in your tests, use the `DependencyInjectionDataSourceAttribute<TScope>` helper class to set up your own DI container. See the [Dependency Injection guide](./dependency-injection.md) for complete details and examples.

## TestBuilderContext

In addition to `TestContext`, TUnit also provides `TestBuilderContext` which is available during the test discovery and building phase. This is particularly useful when you need context information in data generators or other scenarios that run before test execution.

### When to Use TestBuilderContext vs TestContext

**Use `TestBuilderContext.Current` when:**
- Writing data generators that need test information
- During test discovery phase
- In scenarios that run before `TestContext` is available
- When you need to pass data from discovery time to execution time

**Use `TestContext.Current` when:**
- During test execution
- In test methods, Before/After hooks
- When you need test results or execution-specific information
- When accessing test output writers

### Accessing TestBuilderContext

```csharp
public static IEnumerable<object[]> MyDataGenerator()
{
    var context = TestBuilderContext.Current;
    if (context != null)
    {
        // Access test information during data generation
        Console.WriteLine($"Generating data for: {context.TestMethodName}");
        Console.WriteLine($"Test class: {context.ClassInformation?.Type.Name}");
        Console.WriteLine($"Assembly: {context.ClassInformation?.Assembly.Name}");
        
        // Store data for later use during test execution
        context.StateBag["GenerationTime"] = DateTime.Now;
    }
    
    yield return new object[] { 1, 2, 3 };
}
```

### Sharing Data Between Discovery and Execution

The `StateBag` property on `TestBuilderContext` is carried forward to `TestContext`, allowing you to pass data from discovery time to execution time:

```csharp
// In your data generator
public static IEnumerable<object[]> TestData()
{
    var builderContext = TestBuilderContext.Current;
    if (builderContext != null)
    {
        builderContext.StateBag["DataGeneratedAt"] = DateTime.Now;
        builderContext.StateBag["GeneratorVersion"] = "1.0";
    }
    
    yield return new object[] { "test" };
}

// In your test
[Test]
[MethodDataSource(nameof(TestData))]
public void MyTest(string value)
{
    // Access the data stored during generation
    var generatedAt = TestContext.Current.StateBag["DataGeneratedAt"];
    var version = TestContext.Current.StateBag["GeneratorVersion"];
    
    Console.WriteLine($"Data was generated at: {generatedAt}");
}
```

### Available Properties

`TestBuilderContext` provides:
- `TestMethodName` - The name of the test method being built
- `ClassInformation` - Full information about the test class including:
  - `Type` - The test class type
  - `Assembly` - Assembly information
  - `Namespace` - The namespace
  - Properties, parameters, and more
- `MethodInformation` - Full information about the test method
- `StateBag` - A dictionary for storing custom data
- `Events` - Test events that can be subscribed to

Note: `TestBuilderContext.Current` will be `null` if accessed outside of test discovery/building phase.
