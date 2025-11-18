# Test Context

All tests have a `TestContext` object available to them.

This can be accessed statically via `TestContext.Current`.

Here you can see information about the test, including things like the test name, containing class, custom properties, categories, etc.

This can be useful if you want something like a generic `AfterEachTest` for all tests, but with logic to execute for only certain tests.

e.g.
```csharp
if (TestContext.Current?.Metadata.TestDetails.CustomProperties.ContainsKey("SomeProperty") == true)
{
    // Do something
}
```

The context also has a `Results` object. You'll notice this is nullable. This will be null until you're in the context of a `AfterEachTest` method. That's because the `Results` can only be set after a test has finished.

These results can be handy when you're cleaning up, but maybe only want to do something if a test failed.

e.g.
```csharp
if (TestContext.Current?.Result?.State == TestState.Failed)
{
    // Take a screenshot?
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
