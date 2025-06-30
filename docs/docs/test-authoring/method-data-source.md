# Method Data Sources

A limitation of passing data in with `[Arguments(...)]` is that the data must be `constant` values. For example, we can't new up an object and pass it into this attribute as an argument. This is a constraint of the language and we can't change that.

If we want test data represented in the form of objects, or just to use something that isn't a constant, we can declare a test data source.

## AOT-Compatible Method Data Sources

TUnit's AOT-only mode requires **static** method data sources for compile-time safety and performance. Methods must be static and use compile-time resolvable patterns.

`MethodDataSource` has two options:
- If you pass in one argument, this is the method name containing your data. TUnit will assume this is in the current test class.
- If you pass in two arguments, the first should be the `Type` of the class containing your test source data method, and the second should be the name of the method.

:::warning AOT Requirement
Method data sources **must be static** for AOT compatibility. Instance methods will generate compile-time errors in AOT mode.
:::

For reference types, methods should return a `Func<T>` rather than just a `T`, and make sure that `Func<>` returns a `new T()` - This ensures each test has its own instance of that object and tests aren't sharing objects which could lead to unintended side effects.

:::info
Returning a `Func<T>` ensures that each test gets a fresh object.  
If you return a reference to the same object, tests may interfere with each other.
:::

Here's an example returning a simple object:

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTestProject;

public record AdditionTestData(int Value1, int Value2, int ExpectedResult);

public static class MyTestDataSources
{
    public static Func<AdditionTestData> AdditionTestData()
    {
        return () => new AdditionTestData(1, 2, 3);
    }
}

public class MyTestClass
{
    [Test]
    [MethodDataSource(typeof(MyTestDataSources), nameof(MyTestDataSources.AdditionTestData))]
    public async Task MyTest(AdditionTestData additionTestData)
    {
        var result = Add(additionTestData.Value1, additionTestData.Value2);

        await Assert.That(result).IsEqualTo(additionTestData.ExpectedResult);
    }

    private int Add(int x, int y)
    {
        return x + y;
    }
}
```

This can also accept tuples if you don't want to create lots of new types within your test assembly:

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTestProject;

public static class MyTestDataSources
{
    public static Func<(int, int, int)> AdditionTestData()
    {
        return () => (1, 2, 3);
    }
}

public class MyTestClass
{
    [Test]
    [MethodDataSource(typeof(MyTestDataSources), nameof(MyTestDataSources.AdditionTestData))]
    public async Task MyTest(int value1, int value2, int expectedResult)
    {
        var result = Add(value1, value2);

        await Assert.That(result).IsEqualTo(expectedResult);
    }

    private int Add(int x, int y)
    {
        return x + y;
    }
}
```

This attribute can also accept `IEnumerable<>`. For each item returned, a new test will be created with that item passed in to the parameters. Again, if using a reference type, return an `IEnumerable<Func<T>>` and make sure each `Func<>` returns a `new T()`

Here's an example where the test would be invoked 3 times:

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTestProject;

public record AdditionTestData(int Value1, int Value2, int ExpectedResult);

public static class MyTestDataSources
{
    public static IEnumerable<Func<AdditionTestData>> AdditionTestData()
    {
        yield return () => new AdditionTestData(1, 2, 3);
        yield return () => new AdditionTestData(2, 2, 4);
        yield return () => new AdditionTestData(5, 5, 10);
    }
}

public class MyTestClass
{
    [Test]
    [MethodDataSource(typeof(MyTestDataSources), nameof(MyTestDataSources.AdditionTestData))]
    public async Task MyTest(AdditionTestData additionTestData)
    {
        var result = Add(additionTestData.Value1, additionTestData.Value2);

        await Assert.That(result).IsEqualTo(additionTestData.ExpectedResult);
    }

    private int Add(int x, int y)
    {
        return x + y;
    }
}
```

This can also accept tuples if you don't want to create lots of new types within your test assembly:

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTestProject;

public static class MyTestDataSources
{
    public static IEnumerable<Func<(int, int, int)>> AdditionTestData()
    {
        yield return () => (1, 2, 3);
        yield return () => (2, 2, 4);
        yield return () => (5, 5, 10);
    }
}

public class MyTestClass
{
    [Test]
    [MethodDataSource(typeof(MyTestDataSources), nameof(MyTestDataSources.AdditionTestData))]
    public async Task MyTest(int value1, int value2, int expectedResult)
    {
        var result = Add(value1, value2);

        await Assert.That(result).IsEqualTo(expectedResult);
    }

    private int Add(int x, int y)
    {
        return x + y;
    }
}
```

## Async Data Sources (AOT-Compatible)

TUnit's AOT mode supports async data sources with `IAsyncEnumerable<T>` for scenarios requiring asynchronous data loading:

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using System.Runtime.CompilerServices;

namespace MyTestProject;

public record AsyncTestData(int Id, string Name, DateTime CreatedAt);

public static class AsyncTestDataSources
{
    // AOT-compatible async data source with cancellation support
    public static async IAsyncEnumerable<Func<AsyncTestData>> GetAsyncTestData(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        for (int i = 1; i <= 3; i++)
        {
            ct.ThrowIfCancellationRequested();
            
            // Simulate async data loading (database, API, etc.)
            await Task.Delay(10, ct);
            
            yield return () => new AsyncTestData(
                Id: i, 
                Name: $"Item_{i}", 
                CreatedAt: DateTime.UtcNow.AddDays(-i)
            );
        }
    }

    // Simple async enumerable returning tuples
    public static async IAsyncEnumerable<(int, string)> GetSimpleAsyncData(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.Delay(1, ct); // Simulate async work
        yield return (1, "first");
        yield return (2, "second");
        yield return (3, "third");
    }
}

public class AsyncDataSourceTests
{
    [Test]
    [MethodDataSource(typeof(AsyncTestDataSources), nameof(AsyncTestDataSources.GetAsyncTestData))]
    public async Task TestWithAsyncComplexData(AsyncTestData testData)
    {
        await Assert.That(testData.Id).IsGreaterThan(0);
        await Assert.That(testData.Name).StartsWith("Item_");
        await Assert.That(testData.CreatedAt).IsLessThan(DateTime.UtcNow);
    }

    [Test]
    [MethodDataSource(typeof(AsyncTestDataSources), nameof(AsyncTestDataSources.GetSimpleAsyncData))]
    public async Task TestWithAsyncSimpleData(int id, string name)
    {
        await Assert.That(id).IsInRange(1, 3);
        await Assert.That(name).IsNotEmpty();
    }
}
```

:::tip Performance Note
Async data sources are generated with strongly-typed delegates in AOT mode, providing excellent performance while maintaining full async/await support and cancellation token handling.
:::
EOF < /dev/null
