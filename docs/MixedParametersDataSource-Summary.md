# MixedParametersDataSource - Quick Reference

## What is it?

`[MixedParametersDataSource]` allows you to apply different data source attributes to individual test method parameters, automatically generating all possible combinations (Cartesian product).

## Quick Start

```csharp
[Test]
[MixedParametersDataSource]
public async Task MyTest(
    [Arguments(1, 2, 3)] int x,
    [MethodDataSource(nameof(GetStrings))] string y)
{
    // Automatically creates 3 × 2 = 6 test cases
    await Assert.That(x).IsIn([1, 2, 3]);
    await Assert.That(y).IsIn(["Hello", "World"]);
}

public static IEnumerable<string> GetStrings()
{
    yield return "Hello";
    yield return "World";
}
```

## Key Benefits

✅ **Maximum Flexibility** - Mix ANY data source types on different parameters
✅ **Automatic Combinations** - Generates Cartesian product automatically
✅ **Clean Syntax** - Data sources defined right on the parameters
✅ **Type Safe** - Full compile-time type checking
✅ **AOT Compatible** - Works with Native AOT compilation

## Supported Data Sources

Apply these to individual parameters:

- `[Arguments(1, 2, 3)]` - Inline values
- `[MethodDataSource(nameof(Method))]` - From method
- `[ClassDataSource<T>]` - Generate instances
- `[CustomDataSource]` - Any `IDataSourceAttribute`

## Cartesian Product

With 3 parameters:
- Parameter A: 2 values
- Parameter B: 3 values
- Parameter C: 4 values

**Result**: 2 × 3 × 4 = **24 test cases**

## Common Patterns

### Pattern 1: All Arguments
```csharp
[Test]
[MixedParametersDataSource]
public void Test(
    [Arguments(1, 2)] int a,
    [Arguments("x", "y")] string b)
{
    // 2 × 2 = 4 tests
}
```

### Pattern 2: Mixed Sources
```csharp
[Test]
[MixedParametersDataSource]
public void Test(
    [Arguments(1, 2)] int a,
    [MethodDataSource(nameof(GetData))] string b,
    [ClassDataSource<MyClass>] MyClass c)
{
    // 2 × N × 1 = 2N tests
}
```

### Pattern 3: Multiple Per Parameter
```csharp
[Test]
[MixedParametersDataSource]
public void Test(
    [Arguments(1, 2)]
    [Arguments(3, 4)] int a,  // Combines to 4 values
    [Arguments("x")] string b)
{
    // 4 × 1 = 4 tests
}
```

## When to Use

✅ **Use MixedParametersDataSource when:**
- Different parameters need different data sources
- You want maximum flexibility in data generation
- You need to test all combinations of inputs

❌ **Use alternatives when:**
- All parameters use same type of data source → Consider `[Matrix]`
- You only need specific combinations → Use multiple `[Test]` methods with `[Arguments]`
- Test count would be excessive → Break into smaller tests

## Performance Warning

⚠️ **Be mindful of exponential growth!**

| Params | Values Each | Total Tests |
|--------|-------------|-------------|
| 2 | 3 | 9 |
| 3 | 3 | 27 |
| 4 | 3 | 81 |
| 5 | 3 | 243 |
| 3 | 10 | 1,000 |
| 4 | 10 | 10,000 |

## Full Documentation

See [MixedParametersDataSource.md](MixedParametersDataSource.md) for complete documentation including:
- Advanced scenarios
- Error handling
- AOT compilation details
- Troubleshooting guide
- Real-world examples
