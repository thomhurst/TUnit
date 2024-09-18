---
sidebar_position: 5
---

# Data Source Driven Tests

A limitation of passing data in with `[Arguments(...)]` is that the data must be `constant` values. For example, we can't new up an object and pass it into this attribute as an argument. This is a constraint of the language and we can't change that.

If we want test data represented in the form of objects, or just to use something that isn't a constant, we can declare a test data source.

This can come in 3 forms, with help of the following attributes:
- `[MethodDataSource]`
- `[ClassDataSource]`
- `[ClassConstructorAttribute]` (for constructors only)

## MethodDataSource
This has two options:
- If you pass in one argument, this is the method name containing your data. TUnit will assume this is in the current test class.
- If you pass in two arguments, the first should be the `Type` of the class containing your test source data method, and the second should be the name of the method.

Here's an example returning a simple object:

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTestProject;

public record AdditionTestData(int Value1, int Value2, int ExpectedResult);

public static class MyTestDataSources
{
    public static AdditionTestData AdditionTestData()
    {
        return new AdditionTestData(1, 2, 3);
    }
}

public class MyTestClass
{
    [Test]
    [MethodDataSource(typeof(MyTestDataSources), nameof(MyTestDataSources.AdditionTestData))]
    public async Task MyTest(AdditionTestData additionTestData)
    {
        var result = Add(additionTestData.Value1, additionTestData.Value2);

        await Assert.That(result).Is.EqualTo(additionTestData.ExpectedResult);
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
    public static (int, int, int) AdditionTestData()
    {
        return (1, 2, 3);
    }
}

public class MyTestClass
{
    [Test]
    [MethodDataSource(typeof(MyTestDataSources), nameof(MyTestDataSources.AdditionTestData))]
    public async Task MyTest(int value1, int value2, int expectedResult)
    {
        var result = Add(value1, value2);

        await Assert.That(result).Is.EqualTo(expectedResult);
    }

    private int Add(int x, int y)
    {
        return x + y;
    }
}
```

This attribute can also accept `IEnumerable<>`. For each item returned, a new test will be created with that item passed in to the parameters.

Here's an example where the test would be invoked 3 times:

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTestProject;

public record AdditionTestData(int Value1, int Value2, int ExpectedResult);

public static class MyTestDataSources
{
    public static IEnumerable<AdditionTestData> AdditionTestData()
    {
        yield return new AdditionTestData(1, 2, 3);
        yield return new AdditionTestData(2, 2, 4);
        yield return new AdditionTestData(5, 5, 10);
    }
}

public class MyTestClass
{
    [Test]
    [MethodDataSource(typeof(MyTestDataSources), nameof(MyTestDataSources.AdditionTestData))]
    public async Task MyTest(AdditionTestData additionTestData)
    {
        var result = Add(additionTestData.Value1, additionTestData.Value2);

        await Assert.That(result).Is.EqualTo(additionTestData.ExpectedResult);
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
    public static IEnumerable<(int, int, int)> AdditionTestData()
    {
        yield return (1, 2, 3);
        yield return (2, 2, 4);
        yield return (5, 5, 10);
    }
}

public class MyTestClass
{
    [Test]
    [MethodDataSource(typeof(MyTestDataSources), nameof(MyTestDataSources.AdditionTestData))]
    public async Task MyTest(int value1, int value2, int expectedResult)
    {
        var result = Add(value1, value2);

        await Assert.That(result).Is.EqualTo(expectedResult);
    }

    private int Add(int x, int y)
    {
        return x + y;
    }
}
```

## ClassDataSource

The `ClassDataSource` attribute is used to instantiate and inject in new classes as parameters to your tests and/or test classes.

The attribute takes a generic type argument, which is the type of data you want to inject into your test.

It also takes an optional `Shared` argument, controlling whether you want to share the instance among other tests.
This could be useful for times where it's very intensive to spin up lots of objects, and you instead want to share that same instance across many tests.

Ideally don't manipulate the state of this object within your tests if your object is shared. Because of concurrency, it's impossible to know which test will run in which order, and so your tests could become flaky and undeterministic.

Options are:

### Shared = SharedType.None
The instance is not shared ever. A new one will be created for you.

### Shared = SharedType.Globally
The instance is shared globally for every test that also uses this setting, meaning it'll always be the same instance.

### Shared = SharedType.ForClass
The instance is shared for every test in the same class as itself, that also has this setting.

### Shared = SharedType.Keyed
When using this, you must also populate the `Key` argument on the attribute.

The instance is shared for every test that also has this setting, and also uses the same key.

```csharp
public class MyTestClass
{
    [Test]
    [ClassDataSource<SomeClass>(Shared = SharedType.Globally)]
    public void MyTest(SomeClass value)
    {
    }

    public record SomeClass
    {
        // Some properties!
    }
}
```

## ClassConstructorAttribute
See [Class Constructor Helpers](../tutorial-extras/class-constructors.md)