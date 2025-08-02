# Property Injection

TUnit's AOT-compatible property injection system makes it easy to initialize properties on your test class with compile-time safety and excellent performance.

Your properties must be marked with the `required` keyword and then simply place a data attribute on it.
The required keyword keeps your code clean and correct. If a property isn't passed in, you'll get a compiler warning, so you know something has gone wrong. It also gets rid of any pesky nullability warnings.

## AOT-Compatible Property Attributes

Supported attributes for properties in AOT mode:
- **Argument** - Compile-time constant values
- **MethodDataSource** - Static method data sources  
- **ClassDataSource** - Static class-based data sources
- **DataSourceGeneratorAttribute** - Source-generated data (first item only)
- **DataSourceForProperty** - Dependency injection with service provider

The AOT system generates strongly-typed property setters at compile time, eliminating reflection overhead and ensuring full Native AOT compatibility.

## Async Property Initialization

Properties can implement `IAsyncInitializable` for complex setup scenarios with automatic lifecycle management:

```csharp
using TUnit.Core;

namespace MyTestProject;

public class AsyncPropertyExample : IAsyncInitializable, IAsyncDisposable
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

## Basic Property Injection Examples

```csharp
using TUnit.Core;

namespace MyTestProject;

public class PropertySetterTests
{
    // Compile-time constant injection
    [Arguments("1")]
    public required string Property1 { get; init; }
        
    // Static method data source injection
    [MethodDataSource(nameof(GetMethodData))]
    public required string Property2 { get; init; }
        
    // Class-based data source injection
    [ClassDataSource<InnerModel>]
    public required InnerModel Property3 { get; init; }
    
    // Globally shared data source
    [ClassDataSource<InnerModel>(Shared = SharedType.Globally)]
    public required InnerModel Property4 { get; init; }
    
    // Class-scoped shared data source
    [ClassDataSource<InnerModel>(Shared = SharedType.ForClass)]
    public required InnerModel Property5 { get; init; }
    
    // Keyed shared data source
    [ClassDataSource<InnerModel>(Shared = SharedType.Keyed, Key = "Key")]
    public required InnerModel Property6 { get; init; }
        
    // Source-generated data injection
    [DataSourceGeneratorTests.AutoFixtureGenerator<string>]
    public required string Property7 { get; init; }

    // Service provider dependency injection
    [DataSourceForProperty<AsyncPropertyExample>]
    public required AsyncPropertyExample AsyncService { get; init; }
    
    [Test]
    public async Task Test()
    {
        // All properties are automatically initialized before this test runs
        await Assert.That(Property1).IsEqualTo("1");
        await Assert.That(Property2).IsNotNull();
        await Assert.That(Property3).IsNotNull();
        await Assert.That(AsyncService.IsInitialized).IsTrue();
        
        Console.WriteLine($"Property7: {Property7}");
    }

    // Static data source method for Property2
    public static IEnumerable<string> GetMethodData()
    {
        yield return "method_data_1";
        yield return "method_data_2";
    }
}

// Example model for ClassDataSource
public class InnerModel
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}
```

