# Matrix Tests

The Matrix data source is a way to specify different arguments per parameter, and then generate every possible combination of all of those arguments.

Now bear in mind, that as your number of arguments and/or parameters increase, that the number of test cases will grow exponentially. This means you could very quickly get into the territory of generating thousands of test cases. So use it with caution.

For our arguments, we'll add a `[Matrix]` attribute. Instead of this being added to the test method, it's added to the parameters themselves.

And for the test method, we'll add a `[MatrixDataSource]` attribute which contains the logic to extract out all the data from those parameter Matrix attributes.

Here's an example:

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Extensions.Is;
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [MatrixDataSource]
    public async Task MyTest(
        [Matrix(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)] int value1,
        [Matrix(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)] int value2
        )
    {
        var result = Add(value1, value2);

        await Assert.That(result).IsPositive();
    }

    private int Add(int x, int y)
    {
        return x + y;
    }
}
```

That will generate 100 test cases. 10 different values for value1, and 10 different values for value2. 10\*10 is 100.

## Matrix Range

You can also use the `[MatrixRange<T>]` for numerical types. It will generated a range between the minimum and maximum, with an optional step parameter to define how far to step between each value. By default, this is 1.

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Extensions.Is;
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [MatrixDataSource]
    public async Task MyTest(
        [MatrixRange<int>(1, 10)] int value1,
        [MatrixRange<int>(1, 10)] int value2
        )
    {
        var result = Add(value1, value2);

        await Assert.That(result).IsPositive();
    }

    private int Add(int x, int y)
    {
        return x + y;
    }
}
```

## Matrix Method

You can also specify a method that will return an `IEnumerable<T>` of values.

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Extensions.Is;
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [MatrixDataSource]
    public async Task MyTest(
        [MatrixRange<int>(1, 10)] int value1,
        [MatrixMethod(nameof(Numbers))] int value2
        )
    {
        var result = Add(value1, value2);

        await Assert.That(result).IsPositive();
    }

    private int Add(int x, int y)
    {
        return x + y;
    }

    private IEnumerable<int> Numbers()
    {
        yield return 1;
        yield return 2;
        yield return 3;
        yield return 4;
        yield return 5;
        yield return 6;
        yield return 7;
        yield return 8;
        yield return 9;
        yield return 10;
    }
}
```

## Matrix Exclusions

You can also add a `[MatrixExclusion(...)]` attribute to your tests.
This works similar to the `[Arguments(...)]` attribute, and if objects match a generated matrix test case, it'll be ignored.

This helps you exclude specific one-off scenarios without having to complicate your tests with `if` conditions.

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Extensions.Is;
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [MatrixDataSource]
    [MatrixExclusion(1, 1)]
    [MatrixExclusion(2, 2)]
    [MatrixExclusion(3, 3)]
    public async Task MyTest(
        [MatrixRange<int>(1, 3)] int value1,
        [MatrixRange<int>(1, 3)] int value2
        )
    {
        ...
    }
}
```

Whereas the above Matrix would usually generate: 
- 1, 1
- 1, 2
- 1, 3
- 2, 1
- 2, 2
- 2, 3
- 3, 1
- 3, 2
- 3, 3

Because of the exclusion attributes, it'll only generate:
- 1, 2
- 1, 3
- 2, 1
- 2, 3
- 3, 1
- 3, 2
