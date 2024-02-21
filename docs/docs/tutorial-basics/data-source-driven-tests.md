---
sidebar_position: 4
---

# Data Source Driven Tests

A limitation of passing data into the `[DataDrivenTest]` is that the data must be `constant` values. For example, we can't new up an object and pass it into this attribute as an argument. This is a constraint of the language and we can't change that.

If we want test data represented in the form of objects, or just to use something that isn't a constant, we can declare a test data source. This is a `public static` method that returns your object.


Instead of the `[Test]` or `[DataDrivenTest]` attributes, we'll use a `[DataSourceDrivenTest]` attribute.
This has two options:
- If you pass in one argument, this is the method name containing your data. TUnit will assume this is in the current test class.
- If you pass in two arguments, the first should be the `Type` of the class containing your test source data method, and the second should be the name of the method.

Your method can also either return your object type, or an `IEnumerable` of your object type, and your test will be executed multiple times, with each object from the `IEnumerable`.

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
    [DataSourceDrivenTest(typeof(MyTestDataSources), nameof(MyTestDataSources.AdditionTestData))]
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

And here's an example with an `IEnumerable`:

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
    [DataSourceDrivenTest(typeof(MyTestDataSources), nameof(MyTestDataSources.AdditionTestData))]
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
