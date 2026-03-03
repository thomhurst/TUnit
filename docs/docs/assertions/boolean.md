---
sidebar_position: 3.5
---

# Boolean Assertions

TUnit provides simple, expressive assertions for testing boolean values. These assertions work with both `bool` and `bool?` (nullable boolean) types.

## Basic Boolean Assertions

### IsTrue

Tests that a boolean value is `true`:

```csharp
[Test]
public async Task Value_Is_True()
{
    var isValid = ValidateInput("test@example.com");
    await Assert.That(isValid).IsTrue();

    var hasPermission = user.HasPermission("write");
    await Assert.That(hasPermission).IsTrue();
}
```

### IsFalse

Tests that a boolean value is `false`:

```csharp
[Test]
public async Task Value_Is_False()
{
    var isExpired = CheckIfExpired(futureDate);
    await Assert.That(isExpired).IsFalse();

    var isEmpty = list.Count == 0;
    await Assert.That(isEmpty).IsFalse();
}
```

## Alternative: Using IsEqualTo

You can also use `IsEqualTo()` for boolean comparisons:

```csharp
[Test]
public async Task Using_IsEqualTo()
{
    var result = PerformCheck();

    await Assert.That(result).IsEqualTo(true);
    // Same as: await Assert.That(result).IsTrue();

    await Assert.That(result).IsEqualTo(false);
    // Same as: await Assert.That(result).IsFalse();
}
```

However, `IsTrue()` and `IsFalse()` are more expressive and recommended for boolean values.

## Nullable Booleans

Both assertions work with nullable booleans (`bool?`):

```csharp
[Test]
public async Task Nullable_Boolean_True()
{
    bool? result = GetOptionalFlag();

    await Assert.That(result).IsTrue();
    // This asserts both:
    // 1. result is not null
    // 2. result.Value is true
}

[Test]
public async Task Nullable_Boolean_False()
{
    bool? result = GetOptionalFlag();

    await Assert.That(result).IsFalse();
    // This asserts both:
    // 1. result is not null
    // 2. result.Value is false
}
```

### Null Nullable Booleans

If a nullable boolean is `null`, both `IsTrue()` and `IsFalse()` will fail:

```csharp
[Test]
public async Task Nullable_Boolean_Null()
{
    bool? result = null;

    // These will both fail:
    // await Assert.That(result).IsTrue();  // ❌ Fails - null is not true
    // await Assert.That(result).IsFalse(); // ❌ Fails - null is not false

    // Check for null first:
    await Assert.That(result).IsNull();
}
```

## Chaining Boolean Assertions

Boolean assertions can be chained with other assertions:

```csharp
[Test]
public async Task Chained_With_Other_Assertions()
{
    bool? flag = GetFlag();

    await Assert.That(flag)
        .IsNotNull()
        .And.IsTrue();
}
```

## Practical Examples

### Validation Results

```csharp
[Test]
public async Task Email_Validation()
{
    var isValid = EmailValidator.Validate("test@example.com");
    await Assert.That(isValid).IsTrue();

    var isInvalid = EmailValidator.Validate("not-an-email");
    await Assert.That(isInvalid).IsFalse();
}
```

### Permission Checks

```csharp
[Test]
public async Task User_Permissions()
{
    var user = await GetUserAsync("alice");

    await Assert.That(user.CanRead).IsTrue();
    await Assert.That(user.CanWrite).IsTrue();
    await Assert.That(user.CanDelete).IsFalse();
}
```

### State Flags

```csharp
[Test]
public async Task Service_State()
{
    var service = new BackgroundService();

    await Assert.That(service.IsRunning).IsFalse();

    await service.StartAsync();

    await Assert.That(service.IsRunning).IsTrue();
}
```

### Feature Flags

```csharp
[Test]
public async Task Feature_Toggles()
{
    var config = LoadConfiguration();

    await Assert.That(config.EnableNewFeature).IsTrue();
    await Assert.That(config.EnableBetaFeature).IsFalse();
}
```

## Tip: Prefer Specific Assertions

When testing the boolean result of a comparison, use the specific assertion instead for clearer failure messages:

```csharp
[Test]
public async Task Prefer_Specific_Assertions()
{
    var count = GetCount();

    // Less clear — failure message says "expected true but was false":
    await Assert.That(count > 0).IsTrue();

    // More clear — failure message shows the actual value:
    await Assert.That(count).IsGreaterThan(0);
}
```

Use `IsTrue()` / `IsFalse()` for actual boolean values and flags. For comparisons, collections, strings, and types, TUnit provides [dedicated assertions](collections.md) with better failure messages.

## See Also

- [Equality & Comparison](equality-and-comparison.md) - General equality testing
- [Null & Default](null-and-default.md) - Testing for null values
- [Collections](collections.md) - Collection-specific boolean tests (All, Any)
- [Type Assertions](types.md) - Type checking instead of `is` checks
