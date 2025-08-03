# Test Context

All tests have a `TestContext` object available to them.

This can be accessed statically via `TestContext.Current`.

Here you can see information about the test, including things like the test name, containing class, custom properties, categories, etc.

This can be useful if you want something like a generic `AfterEachTest` for all tests, but with logic to execute for only certain tests.

e.g.
```csharp
if (TestContext.Current.TestInformation.CustomProperties.ContainsKey("SomeProperty"))
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

## Service Provider Integration

`TestContext` provides access to dependency injection services through the `GetService<T>()` and `GetRequiredService<T>()` methods. This allows you to access registered services within your tests, hooks, and custom extensions.

### Accessing Services

```csharp
[Test]
public async Task DatabaseTest()
{
    // Get an optional service (returns null if not registered)
    var logger = TestContext.Current?.GetService<ILogger<DatabaseTest>>();
    logger?.LogInformation("Starting database test");
    
    // Get a required service (throws if not registered)
    var dbContext = TestContext.Current!.GetRequiredService<ApplicationDbContext>();
    
    // Use the service
    var users = await dbContext.Users.ToListAsync();
    await Assert.That(users).IsNotEmpty();
}
```

### Common Use Cases

#### 1. Accessing Loggers

```csharp
[Before(HookType.Test)]
public void LogTestStart()
{
    var logger = TestContext.Current?.GetService<ILogger>();
    logger?.LogInformation("Test {TestName} starting", 
        TestContext.Current?.TestDetails.TestName);
}
```

#### 2. Working with Scoped Services

```csharp
[Test]
public async Task ScopedServiceTest()
{
    // Each test gets its own scope, so scoped services are isolated
    var service1 = TestContext.Current!.GetRequiredService<IScopedService>();
    var service2 = TestContext.Current!.GetRequiredService<IScopedService>();
    
    // These will be the same instance within the test
    await Assert.That(ReferenceEquals(service1, service2)).IsTrue();
}
```

#### 3. Configuration Access

```csharp
[Test]
public async Task ConfigurationTest()
{
    var configuration = TestContext.Current?.GetService<IConfiguration>();
    var apiKey = configuration?["ApiSettings:Key"];
    
    await Assert.That(apiKey).IsNotNull();
}
```

### Service Provider in Custom Extensions

When implementing custom test executors or hook executors, you can use the service provider:

```csharp
public class DatabaseTransactionExecutor : ITestExecutor
{
    public async Task ExecuteAsync(TestContext context, Func<Task> testBody)
    {
        // Get database context from DI
        var dbContext = context.GetRequiredService<ApplicationDbContext>();
        
        using var transaction = await dbContext.Database.BeginTransactionAsync();
        
        try
        {
            await testBody();
            await transaction.RollbackAsync(); // Keep tests isolated
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

### Integration with Test Lifecycle

The service provider is available throughout the test lifecycle:

```csharp
public class ServiceIntegrationTests
{
    [Before(HookType.Class)]
    public static async Task ClassSetup()
    {
        // Services available in class-level hooks via the hook context
        var context = ClassHookContext.Current;
        var cache = context?.GetService<IMemoryCache>();
        cache?.Set("test-data", await LoadTestData());
    }
    
    [Before(HookType.Test)]
    public async Task TestSetup()
    {
        // Services available in test-level hooks
        var cache = TestContext.Current?.GetService<IMemoryCache>();
        var testData = cache?.Get<TestData>("test-data");
    }
    
    [Test]
    public async Task ActualTest()
    {
        // Services available in test methods
        var service = TestContext.Current!.GetRequiredService<IBusinessService>();
        var result = await service.PerformOperation();
        await Assert.That(result).IsNotNull();
    }
}
```

### Best Practices

1. **Use GetRequiredService for Essential Services**
   ```csharp
   // Good - Fails fast if service is missing
   var critical = TestContext.Current!.GetRequiredService<ICriticalService>();
   
   // Less ideal - Might hide configuration issues
   var critical = TestContext.Current?.GetService<ICriticalService>() 
       ?? throw new InvalidOperationException("Service not found");
   ```

2. **Null Check When Using GetService**
   ```csharp
   var optional = TestContext.Current?.GetService<IOptionalService>();
   if (optional != null)
   {
       await optional.DoSomething();
   }
   ```

3. **Consider Service Lifetime**
   ```csharp
   // Singleton services persist across tests
   var singleton = TestContext.Current?.GetService<ISingletonService>();
   
   // Scoped services are unique per test
   var scoped = TestContext.Current?.GetService<IScopedService>();
   
   // Transient services are created each time
   var transient1 = TestContext.Current?.GetService<ITransientService>();
   var transient2 = TestContext.Current?.GetService<ITransientService>();
   // transient1 and transient2 are different instances
   ```

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
        context.ObjectBag["GenerationTime"] = DateTime.Now;
    }
    
    yield return new object[] { 1, 2, 3 };
}
```

### Sharing Data Between Discovery and Execution

The `ObjectBag` property on `TestBuilderContext` is carried forward to `TestContext`, allowing you to pass data from discovery time to execution time:

```csharp
// In your data generator
public static IEnumerable<object[]> TestData()
{
    var builderContext = TestBuilderContext.Current;
    if (builderContext != null)
    {
        builderContext.ObjectBag["DataGeneratedAt"] = DateTime.Now;
        builderContext.ObjectBag["GeneratorVersion"] = "1.0";
    }
    
    yield return new object[] { "test" };
}

// In your test
[Test]
[MethodDataSource(nameof(TestData))]
public void MyTest(string value)
{
    // Access the data stored during generation
    var generatedAt = TestContext.Current.ObjectBag["DataGeneratedAt"];
    var version = TestContext.Current.ObjectBag["GeneratorVersion"];
    
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
- `ObjectBag` - A dictionary for storing custom data
- `Events` - Test events that can be subscribed to

Note: `TestBuilderContext.Current` will be `null` if accessed outside of test discovery/building phase.
