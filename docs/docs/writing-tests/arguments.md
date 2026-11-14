# Data Driven Tests

Inject compile-time known data via `[Arguments(...)]` attributes. The attribute takes an array of arguments that must match the test method's parameter count and types. Multiple `[Arguments]` attributes repeat the test once per attribute:

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [Arguments(1, 1, 2)]
    [Arguments(1, 2, 3)]
    [Arguments(2, 2, 4)]
    [Arguments(4, 3, 7)]
    [Arguments(5, 5, 10)]
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

## Converting String Arguments

A string argument can be passed to a parameter of another type when that type implements `IParsable<TSelf>` (such as `DateTime`, `Guid` or `TimeSpan`) or declares a `[TypeConverter]` that converts from strings. Conversions use the invariant culture.

```csharp
using System;
using System.ComponentModel;
using System.Globalization;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTestProject;

[TypeConverter(typeof(EmailConverter))]
public sealed record Email(string Value);

public sealed class EmailConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        => value is string text ? new Email(text) : base.ConvertFrom(context, culture, value);
}

public class EmailTests
{
    [Test]
    [Arguments("info@example.com")]
    public async Task ParsesEmail(Email email)
    {
        await Assert.That(email.Value).IsEqualTo("info@example.com");
    }
}
```

The converter is constructed directly by generated code, so this works with Native AOT. It needs a public parameterless constructor or a public constructor that takes the target `Type`. A converter declared on a base type is also used.

## Test Case Metadata

The `[Arguments]` attribute supports optional properties to customize individual test cases:

### Custom Display Names

Use the `DisplayName` property to provide a human-readable name for each test case:

```csharp
[Test]
[Arguments(1, 1, 2, DisplayName = "One plus one equals two")]
[Arguments(0, 0, 0, DisplayName = "Zero plus zero equals zero")]
[Arguments(-1, 1, 0, DisplayName = "Negative and positive cancel out")]
public async Task Addition(int a, int b, int expected)
{
    await Assert.That(a + b).IsEqualTo(expected);
}
```

Display names support parameter substitution using `$paramName` or positional `$arg1`, `$arg2` syntax:

```csharp
[Test]
[Arguments(2, 3, 5, DisplayName = "Adding $a + $b = $expected")]
[Arguments(10, 5, 15, DisplayName = "$arg1 + $arg2 = $arg3")]
public async Task AdditionWithSubstitution(int a, int b, int expected)
{
    await Assert.That(a + b).IsEqualTo(expected);
}
```

### Categories

Apply categories to specific test cases for filtering:

```csharp
[Test]
[Arguments(100, 50, Categories = new[] { "LargeNumbers", "Performance" })]
[Arguments(1, 1, Categories = new[] { "SmallNumbers", "Smoke" })]
public async Task CategorizedTests(int a, int b)
{
    await Assert.That(a + b).IsGreaterThan(0);
}
```

### Skipping Test Cases

Use the `Skip` property to skip specific test cases:

```csharp
[Test]
[Arguments("Chrome", "120")]
[Arguments("Firefox", "121")]
[Arguments("Safari", "17", Skip = "Safari testing not available in CI")]
public async Task BrowserTest(string browser, string version)
{
    // Test implementation
}
```

### Combining Properties

All properties can be combined:

```csharp
[Test]
[Arguments("admin", "secret123", DisplayName = "Admin login", Categories = new[] { "Auth", "Admin" })]
[Arguments("guest", "guest", DisplayName = "Guest login", Categories = new[] { "Auth" })]
[Arguments("", "", DisplayName = "Empty credentials", Skip = "Edge case not implemented")]
public async Task LoginTest(string username, string password)
{
    // Test implementation
}
```

:::tip
For dynamic test data or complex objects, use [Method Data Sources](./method-data-source.md) with [TestDataRow](./test-data-row.md) for the same metadata capabilities.
:::
