# Pairwise Tests

[Matrix tests](matrix-tests.md) run every combination of parameter values, so the number of test cases grows exponentially with the number of parameters.
Pairwise testing (also known as *all-pairs* testing) is a well-known way to keep that under control: instead of every combination, it generates a much smaller set of test cases in which **every pair of values, across every pair of parameters, appears at least once**.

Most bugs are triggered by a single value or by the interaction of two parameters, so pairwise testing catches a large share of them with far fewer test cases.

To use it, swap `[MatrixDataSource]` for `[PairwiseDataSource]`. The parameters are declared exactly as for matrix tests:

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [PairwiseDataSource]
    public async Task MyTest(
        bool largeArrayPool,
        bool excessSpan,
        [Matrix(0, 99, 100, 101)] int sizeHint,
        [Matrix(1, 2, 3, 100)] int stepCount)
    {
        var total = sizeHint + stepCount;

        await Assert.That(total).IsPositive();
    }
}
```

`[MatrixDataSource]` would generate 2 × 2 × 4 × 4 = 64 test cases for this test. `[PairwiseDataSource]` generates 16, and every combination of values for any two of the parameters (for example `excessSpan: true` with `sizeHint: 101`) is still tested.

The savings grow quickly with more parameters:

| Parameter value counts | `[MatrixDataSource]` | `[PairwiseDataSource]` |
|---|---:|---:|
| 2 × 2 × 4 × 4 | 64 | 16 |
| 2 × 2 × 3 × 3 × 2 | 72 | 10 |
| 3 × 3 × 3 × 3 | 81 | 9 |
| 3 × 4 × 5 × 6 | 360 | 30 |
| 10 × 10 × 10 | 1,000 | 106 |
| 2 (×10 parameters) | 1,024 | 9 |

:::info
With two parameters, pairwise and matrix generate the same test cases, because every combination *is* a pair.
Pairwise starts to pay off from three parameters.
:::

:::warning
Pairwise testing does not test every combination. If a bug only shows up for a specific combination of three or more parameter values, a pairwise test may not hit it.
Use `[MatrixDataSource]` when you need exhaustive coverage.
:::

## Parameter values

`[PairwiseDataSource]` reads parameter values the same way as `[MatrixDataSource]`, so everything described in [Matrix Tests](matrix-tests.md) works here too:

- `[Matrix(...)]` and `[Matrix<T>(...)]` with explicit values, including `Excluding = [...]`
- `[MatrixRange<T>(...)]` for numeric ranges
- `[MatrixMethod<T>(...)]` for values returned by a method
- `bool` and `enum` parameters (and their nullable forms) expand to all of their values automatically. For nullable types, `null` is added after the other values.

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTestProject;

public enum Color
{
    Red,
    Green,
    Blue
}

public class MyTestClass
{
    [Test]
    [PairwiseDataSource]
    public async Task MyTest(
        Color color,
        bool? flag,
        [MatrixRange<int>(1, 5)] int number,
        [Matrix("x", "y")] string text,
        [MatrixMethod<MyTestClass>(nameof(Sizes))] int size)
    {
        await Assert.That(number).IsBetween(1, 5);
    }

    public static IEnumerable<int> Sizes()
    {
        yield return 1;
        yield return 10;
        yield return 100;
    }
}
```

This generates 17 test cases instead of 270.

## Exclusions

`[MatrixExclusion(...)]` works with `[PairwiseDataSource]` as well. Exclusions are applied *while* the test cases are generated, not by filtering afterwards, so every pair of values that can still appear in an allowed test case remains covered.

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [PairwiseDataSource]
    [MatrixExclusion(1, 1, 1)]
    [MatrixExclusion(2, 2, 2)]
    [MatrixExclusion(3, 3, 3)]
    public async Task MyTest(
        [Matrix(1, 2, 3)] int value1,
        [Matrix(1, 2, 3)] int value2,
        [Matrix(1, 2, 3)] int value3)
    {
        var allEqual = value1 == value2 && value2 == value3;

        await Assert.That(allEqual).IsFalse();
    }
}
```

## Determinism and seeds

The generated test cases are deterministic: the same parameter values always produce the same test cases, so test IDs are stable between runs and between source-generated and reflection mode.

The algorithm uses a seeded pseudo-random number generator to pick test cases. You can change the seed with the `Seed` property to get a different, equally valid, set of test cases:

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [PairwiseDataSource(Seed = 42)]
    public async Task MyTest(
        [Matrix(1, 2, 3)] int a,
        [Matrix(1, 2, 3, 4)] int b,
        [Matrix(1, 2, 3, 4, 5)] int c)
    {
        await Assert.That(a + b + c).IsPositive();
    }
}
```

:::note
Changing the parameter values (or their order) can change which test cases are generated, not just add or remove the cases for the changed value.
:::

## Limitations

- Like `[MatrixDataSource]`, `[PairwiseDataSource]` only supports test method parameters. It can't be used for class constructor parameters.
- Pairwise coverage only guarantees that every *pair* of values is tested. It does not try to cover combinations of three or more values.
- When many values are combined with heavy `[MatrixExclusion]` sets, discovery may still probe a large search space for unreachable pairs. Prefer fewer values or fewer exclusions when generation is slow.

## Algorithm

The pairwise algorithm is based on Bob Jenkins' [jenny](https://burtleburtle.net/bob/math/jenny.html) tool, by way of NUnit's `PairwiseStrategy` and [Xunit.Combinatorial](https://github.com/AArnott/Xunit.Combinatorial)'s `[PairwiseData]`.
It uses the same default seed as Xunit.Combinatorial, so tests migrated from `[PairwiseData]` generate the same number of test cases.
