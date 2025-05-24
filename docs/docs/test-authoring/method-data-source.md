---
sidebar_position: 5
---

# Method Data Sources

A limitation of passing data in with `[Arguments(...)]` is that the data must be `constant` values. For example, we can't new up an object and pass it into this attribute as an argument. This is a constraint of the language and we can't change that.

If we want test data represented in the form of objects, or just to use something that isn't a constant, we can declare a test data source.

`MethodDataSource` has two options:
- If you pass in one argument, this is the method name containing your data. TUnit will assume this is in the current test class.
- If you pass in two arguments, the first should be the `Type` of the class containing your test source data method, and the second should be the name of the method.

If methods are returning reference types, they should return a `Func<T>` rather than just a `T`, and make sure that `Func<>` returns a `new T()`, and not a reference to an already instantiated object - This ensures each test has its own instance of that object and tests aren't sharing objects which could lead to unintended side effects.

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
