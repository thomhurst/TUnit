---
sidebar_position: 4
---

# Repeating

If you want to repeat a test, add a `[Repeat]` attribute onto your test method or class. This takes an `int` of how many extra times the test should run. Each repeat appears in the test explorer as a separate test instance.

This can be used on base classes and inherited to affect all tests in sub-classes.

## Repeat vs. Retry

`[Repeat(N)]` always runs the test N additional times, unconditionally, regardless of pass or fail. `[Retry(N)]` only re-runs a test when it fails, up to N additional attempts, and stops as soon as it passes. Use `[Repeat]` for consistency and stress testing; use `[Retry]` for flaky test mitigation.

## Example

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [Repeat(3)]
    public async Task Calculation_Is_Consistent()
    {
        var result = Calculator.Add(2, 3);

        await Assert.That(result).IsEqualTo(5);
    }
}
```

This produces 4 test runs in total: the original plus 3 repeats. In the test explorer, they appear as:

- `Calculation_Is_Consistent`
- `Calculation_Is_Consistent (RepeatIndex: 1)`
- `Calculation_Is_Consistent (RepeatIndex: 2)`
- `Calculation_Is_Consistent (RepeatIndex: 3)`

## Global Repeat

In case you want to apply the repeat logic to all tests in a project, you can add the attribute on the assembly level.

```csharp
[assembly: Repeat(3)]
```

Or you can apply the repeat policy on all the tests in a class like this:

```csharp
[Repeat(3)]
public class MyTestClass
{
}
```

The more specific attribute will always override the more general one.
For example, the `[Repeat(3)]` on a method will override the `[Repeat(5)]` on the class,
which in turn will override the `[Repeat(7)]` on the assembly.

So the order of precedence is:
1. Method
1. Class
1. Assembly
