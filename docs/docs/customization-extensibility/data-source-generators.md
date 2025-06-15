# Data Source Generators

TUnit provides several base classes for creating custom data source generators:

## DataSourceGeneratorAttribute

The standard `DataSourceGeneratorAttribute` class uses generic `Type` arguments to keep your data strongly typed. This attribute can be useful to easily populate data in a generic way, and without having to define lots of specific `MethodDataSources`.

If you just need to generate data for a single parameter, you simply return `T`.

If you need to generate data for multiple parameters, you must use a `Tuple<>` return type. E.g. `return (T1, T2, T3)`

Here's an example that uses AutoFixture to generate arguments:

```csharp
using TUnit.Core;

namespace MyTestProject;

public class AutoFixtureGeneratorAttribute<T1, T2, T3> : DataSourceGeneratorAttribute<T1, T2, T3>
{
    public override IEnumerable<Func<(T1, T2, T3)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var fixture = new Fixture();
        
        yield return () => (fixture.Create<T1>(), fixture.Create<T2>(), fixture.Create<T3>());
    }
}

[AutoFixtureGenerator<SomeClass1, SomeClass2, SomeClass3>]
public class MyTestClass(SomeClass1 someClass1, SomeClass2 someClass2, SomeClass3 someClass3)
{
    [Test]
    [AutoFixtureGenerator<int, string, bool>]
    public async Task Test((int value, string value2, bool value3))
    {
        // ...
    }
}


```

## AsyncDataSourceGeneratorAttribute

For data sources that need to perform asynchronous operations (like reading from a database, calling an API, or loading files), TUnit provides `AsyncDataSourceGeneratorAttribute`.

This works similarly to `DataSourceGeneratorAttribute` but allows you to use async/await:

:::warning Performance Consideration
**Important**: AsyncDataSourceGenerator code runs at test discovery time, not test execution time. This means:
- Keep async operations fast and lightweight
- Avoid long-running operations or external dependencies that might be slow/unavailable
- If an async operation hangs, your tests may never be discovered
- Consider caching results if the operation is expensive
- For heavy operations, consider using a regular test method that loads data once and shares it across tests
:::

```csharp
using TUnit.Core;

namespace MyTestProject;

public class DatabaseDataGeneratorAttribute<T> : AsyncDataSourceGeneratorAttribute<T> where T : class
{
    private readonly string _connectionString;
    
    public DatabaseDataGeneratorAttribute(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public override async IAsyncEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var entities = await connection.QueryAsync<T>("SELECT * FROM " + typeof(T).Name);
        
        foreach (var entity in entities)
        {
            yield return () => entity;
        }
    }
}

[Test]
[DatabaseDataGenerator<Customer>("Server=localhost;Database=TestDb;")]
public async Task TestCustomerBehavior(Customer customer)
{
    // Test with real customer data from database
}
```

## NonTypedDataSourceGeneratorAttribute

For scenarios where you need to generate dynamic types or work with libraries that don't have compile-time type information (like AutoFixture), TUnit provides `NonTypedDataSourceGeneratorAttribute`.

This is particularly useful when:
- Working with anonymous types
- Using dynamic type generation libraries
- Creating data where the type isn't known at compile time

```csharp
using TUnit.Core;
using AutoFixture;

namespace MyTestProject;

public class AutoFixtureGeneratorAttribute : NonTypedDataSourceGeneratorAttribute
{
    private readonly Type[] _types;
    
    public AutoFixtureGeneratorAttribute(params Type[] types)
    {
        _types = types;
    }
    
    public override IEnumerable<Func<object?[]>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var fixture = new Fixture();
        
        yield return () => _types.Select(type => fixture.Create(type, new SpecimenContext(fixture))).ToArray();
    }
}

[Test]
[AutoFixtureGenerator(typeof(Customer), typeof(Order), typeof(Product))]
public async Task TestWithDynamicTypes(Customer customer, Order order, Product product)
{
    // AutoFixture will generate test data for all three parameters
}

// You can also use it at the class level
[AutoFixtureGenerator(typeof(DatabaseContext))]
public class RepositoryTests(DatabaseContext context)
{
    [Test]
    public async Task TestRepository()
    {
        // context is populated by AutoFixture
    }
}
```

## Important Notes

### Func Return Pattern
`GenerateDataSources()` could be called multiple times if you have nested loops to generate data within your tests. Because of this, you are required to return a `Func` - This means that tests can create a new object each time for a test case. Otherwise, we'd be pointing to the same object if we were in a nested loop and that could lead to unintended side-effects.

An example could be using a DataSourceGenerator on both the class and the test method, resulting with a loop within a loop.

### TestBuilderContext
Because this could be called multiple times, if you're subscribing to test events and storing state within the attribute, be aware of this and how this could affect disposal etc.

Instead, you can use the `yield return` pattern, and use the `TestBuilderContext` from the `DataGeneratorMetadata` object passed to you.
After each `yield`, the execution is passed back to TUnit, and TUnit will set a new `TestBuilderContext` for you - So as long as you yield each result, you'll get a unique context object for each test case.
The `TestBuilderContext` object exposes `Events` - And you can register a delegate to be invoked on them at the point in the test lifecycle that you wish.

```csharp
public override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
{
    dataGeneratorMetadata.TestBuilderContext.Current; // <-- Initial Context for first test
    
    yield return () => 1;
    
    dataGeneratorMetadata.TestBuilderContext.Current; // <-- This is now a different context object, as we yielded
    dataGeneratorMetadata.TestBuilderContext.Current; // <-- This is still the same as above because it'll only change on a yield
    
    yield return () => 2;
    
    dataGeneratorMetadata.TestBuilderContext.Current; // <-- A new object again
}
```

## Choosing the Right Base Class

- **DataSourceGeneratorAttribute**: Use when you know the types at compile time and want strong typing
- **AsyncDataSourceGeneratorAttribute**: Use when you need to perform async operations (database, API, file I/O)
- **NonTypedDataSourceGeneratorAttribute**: Use when working with dynamic types or type generation libraries
