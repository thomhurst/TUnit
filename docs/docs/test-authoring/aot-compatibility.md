# AOT Compatibility and Generic Tests

TUnit's AOT-only mode provides compile-time safety and performance benefits, but requires specific patterns for advanced scenarios like generic tests and complex data sources.

## Generic Test Instantiation

Generic test classes and methods require explicit type instantiation for AOT compatibility.

### Generic Test Methods

Use the `[GenerateGenericTest]` attribute to specify which type combinations to generate:

```csharp
using TUnit.Core;

namespace MyTestProject;

public class GenericTests
{
    [Test]
    [GenerateGenericTest(typeof(int), typeof(string))]
    [GenerateGenericTest(typeof(long), typeof(bool))]
    [GenerateGenericTest(typeof(double), typeof(char))]
    public async Task GenericTestMethod<T1, T2>()
    {
        // Test logic using T1 and T2
        var value1 = default(T1);
        var value2 = default(T2);
        
        await Assert.That(value1).IsNotNull().Or.IsEqualTo(default(T1));
        await Assert.That(value2).IsNotNull().Or.IsEqualTo(default(T2));
    }
}
```

### Generic Test Classes

Apply `[GenerateGenericTest]` to the class to generate all test methods for specified types:

```csharp
using TUnit.Core;

namespace MyTestProject;

[GenerateGenericTest(typeof(int))]
[GenerateGenericTest(typeof(string))]
[GenerateGenericTest(typeof(DateTime))]
public class GenericTestClass<T>
{
    [Test]
    public async Task TestDefaultValue()
    {
        var defaultValue = default(T);
        
        // For reference types, default should be null
        // For value types, default should be the type's default value
        if (typeof(T).IsValueType)
        {
            await Assert.That(defaultValue).IsNotNull();
        }
        else
        {
            await Assert.That(defaultValue).IsNull();
        }
    }

    [Test]
    [Arguments("test data")]
    public async Task TestWithGenericAndArguments(string input)
    {
        var value = default(T);
        
        await Assert.That(input).IsEqualTo("test data");
        // Can use both generic type T and regular parameters
    }
}
```

## AOT-Compatible Data Sources

### Static Data Sources

Use static methods and properties for AOT-compatible data sources:

```csharp
using TUnit.Core;

namespace MyTestProject;

public class AotDataSourceTests
{
    // Static method data source - AOT compatible
    [Test]
    [MethodDataSource(nameof(GetTestData))]
    public async Task TestWithStaticData(int value, string name)
    {
        await Assert.That(value).IsGreaterThan(0);
        await Assert.That(name).IsNotEmpty();
    }

    public static IEnumerable<object[]> GetTestData()
    {
        yield return new object[] { 1, "first" };
        yield return new object[] { 2, "second" };
        yield return new object[] { 3, "third" };
    }

    // Static property data source - AOT compatible
    [Test]
    [MethodDataSource(nameof(PropertyTestData))]
    public async Task TestWithPropertyData(bool flag, double number)
    {
        await Assert.That(flag).IsTrue().Or.IsFalse(); // Either is valid
        await Assert.That(number).IsGreaterThanOrEqualTo(0.0);
    }

    public static IEnumerable<object[]> PropertyTestData => new[]
    {
        new object[] { true, 1.5 },
        new object[] { false, 2.7 },
        new object[] { true, 0.0 }
    };
}
```

### Async Data Sources with Cancellation

AOT mode supports async data sources with proper cancellation token handling:

```csharp
using TUnit.Core;
using System.Runtime.CompilerServices;

namespace MyTestProject;

public class AsyncDataSourceTests
{
    [Test]
    [MethodDataSource(nameof(GetAsyncTestData))]
    public async Task TestWithAsyncData(int id, string data)
    {
        await Assert.That(id).IsGreaterThan(0);
        await Assert.That(data).StartsWith("data");
    }

    public static async IAsyncEnumerable<object[]> GetAsyncTestData(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        for (int i = 1; i <= 3; i++)
        {
            ct.ThrowIfCancellationRequested();
            
            // Simulate async work
            await Task.Delay(10, ct);
            
            yield return new object[] { i, $"data_{i}" };
        }
    }
}
```

## Advanced Property Injection

### Service Provider Integration

AOT mode includes a built-in service provider for dependency injection:

```csharp
using TUnit.Core;
using TUnit.Core.Services;

namespace MyTestProject;

public class ServiceInjectionTests
{
    [DataSourceForProperty<DatabaseService>(Shared = SharedType.Globally)]
    public required DatabaseService Database { get; init; }

    [DataSourceForProperty<LoggingService>]
    public required LoggingService Logger { get; init; }

    [Test]
    public async Task TestWithInjectedServices()
    {
        // Services are automatically injected before test execution
        await Assert.That(Database).IsNotNull();
        await Assert.That(Logger).IsNotNull();
        
        var result = await Database.QueryAsync("SELECT 1");
        Logger.Log($"Query result: {result}");
        
        await Assert.That(result).IsEqualTo(1);
    }
}

// Example service classes
public class DatabaseService
{
    public async Task<int> QueryAsync(string sql)
    {
        await Task.Delay(1); // Simulate async database call
        return 1;
    }
}

public class LoggingService
{
    public void Log(string message)
    {
        Console.WriteLine($"[LOG] {message}");
    }
}
```

### Async Property Initialization

Properties can implement `IAsyncInitializable` for complex setup:

```csharp
using TUnit.Core;

namespace MyTestProject;

public class AsyncInitializationTests
{
    [DataSourceForProperty<AsyncContainer>]
    public required AsyncContainer Container { get; init; }

    [Test]
    public async Task TestWithAsyncInitializedProperty()
    {
        // Container.InitializeAsync() is called automatically before test
        await Assert.That(Container.IsInitialized).IsTrue();
        await Assert.That(Container.ConnectionString).IsNotEmpty();
    }
}

public class AsyncContainer : IAsyncInitializable, IAsyncDisposable
{
    public bool IsInitialized { get; private set; }
    public string ConnectionString { get; private set; } = "";

    public async Task InitializeAsync()
    {
        // Simulate async initialization
        await Task.Delay(10);
        ConnectionString = "Server=localhost;Database=test";
        IsInitialized = true;
    }

    public async ValueTask DisposeAsync()
    {
        // Cleanup is called automatically after test
        await Task.Delay(1);
        IsInitialized = false;
        ConnectionString = "";
    }
}
```

## Compile-Time Diagnostics

AOT mode provides helpful compile-time diagnostics for common issues:

### Generic Test Diagnostics

```csharp
// ❌ This will generate TUnit0058 error
[Test]
public async Task GenericTest<T>() // Missing [GenerateGenericTest]
{
    var value = default(T);
    await Assert.That(value).IsNotNull().Or.IsNull();
}

// ✅ Correct usage
[Test]
[GenerateGenericTest(typeof(int))]
[GenerateGenericTest(typeof(string))]
public async Task GenericTest<T>()
{
    var value = default(T);
    await Assert.That(value).IsNotNull().Or.IsNull();
}
```


### Data Source Diagnostics

```csharp
public class DataSourceDiagnostics
{
    // ❌ This will generate TUnit0059 error - dynamic data source
    [Test]
    [MethodDataSource(nameof(GetDynamicData))]
    public async Task TestWithDynamicDataSource(object value)
    {
        await Assert.That(value).IsNotNull();
    }

    public IEnumerable<object[]> GetDynamicData()
    {
        // This method uses reflection internally - not AOT compatible
        return SomeReflectionBasedDataGenerator.GetData();
    }

    // ✅ Use static, compile-time known data sources
    [Test]
    [MethodDataSource(nameof(GetStaticData))]
    public async Task TestWithStaticDataSource(string value)
    {
        await Assert.That(value).IsNotNull();
    }

    public static IEnumerable<object[]> GetStaticData()
    {
        yield return new object[] { "static1" };
        yield return new object[] { "static2" };
    }
}
```

## Performance Benefits

The AOT-only mode provides significant performance improvements:

- **2-3x faster test execution** compared to reflection-based approach
- **Zero runtime type introspection** - all types resolved at compile time
- **Minimal memory allocations** through strongly-typed delegates
- **Better code optimization** from the compiler and runtime

## Configuration Reference

Configure AOT behavior through your `.editorconfig` file:

```ini
# TUnit AOT Configuration
tunit.aot_only_mode = true              # Enable AOT-only mode (default: true)
tunit.generic_depth_limit = 5           # Max generic nesting depth (default: 5)  
tunit.enable_property_injection = true  # Enable property DI (default: true)
tunit.enable_valuetask_hooks = true     # Enable ValueTask hooks (default: true)
tunit.enable_verbose_diagnostics = false # Verbose diagnostics (default: false)
tunit.max_generic_instantiations = 10   # Max generic instantiations (default: 10)
tunit.enable_auto_generic_discovery = true # Auto-discover generics (default: true)
```

These settings help balance compilation time, binary size, and functionality based on your project's needs.