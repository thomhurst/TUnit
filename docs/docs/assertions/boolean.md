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

## Testing Conditional Logic

### Logical AND

```csharp
[Test]
public async Task Logical_AND()
{
    var isAdult = age >= 18;
    var hasLicense = CheckLicense(userId);
    var canDrive = isAdult && hasLicense;

    await Assert.That(canDrive).IsTrue();
}
```

### Logical OR

```csharp
[Test]
public async Task Logical_OR()
{
    var isWeekend = dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    var isHoliday = CheckIfHoliday(date);
    var isDayOff = isWeekend || isHoliday;

    await Assert.That(isDayOff).IsTrue();
}
```

### Logical NOT

```csharp
[Test]
public async Task Logical_NOT()
{
    var isExpired = CheckExpiration(token);
    var isValid = !isExpired;

    await Assert.That(isValid).IsTrue();
}
```

## Complex Boolean Expressions

```csharp
[Test]
public async Task Complex_Expression()
{
    var user = GetUser();
    var canAccess = user.IsActive &&
                    !user.IsBanned &&
                    (user.IsPremium || user.HasFreeTrial);

    await Assert.That(canAccess).IsTrue();
}
```

You can also break this down for clarity:

```csharp
[Test]
public async Task Complex_Expression_Broken_Down()
{
    var user = GetUser();

    using (Assert.Multiple())
    {
        await Assert.That(user.IsActive).IsTrue();
        await Assert.That(user.IsBanned).IsFalse();
        await Assert.That(user.IsPremium || user.HasFreeTrial).IsTrue();
    }
}
```

## Comparison with Other Values

When testing boolean results of comparisons, you can often simplify:

```csharp
[Test]
public async Task Comparison_Simplified()
{
    var count = GetCount();

    // Less clear:
    await Assert.That(count > 0).IsTrue();

    // More clear and expressive:
    await Assert.That(count).IsGreaterThan(0);
}
```

Similarly for equality:

```csharp
[Test]
public async Task Equality_Simplified()
{
    var name = GetName();

    // Less clear:
    await Assert.That(name == "Alice").IsTrue();

    // More clear:
    await Assert.That(name).IsEqualTo("Alice");
}
```

Use boolean assertions for actual boolean values and flags, not for comparisons.

## Testing LINQ Queries

```csharp
[Test]
public async Task LINQ_Any()
{
    var numbers = new[] { 1, 2, 3, 4, 5 };

    var hasEven = numbers.Any(n => n % 2 == 0);
    await Assert.That(hasEven).IsTrue();

    var hasNegative = numbers.Any(n => n < 0);
    await Assert.That(hasNegative).IsFalse();
}

[Test]
public async Task LINQ_All()
{
    var numbers = new[] { 2, 4, 6, 8 };

    var allEven = numbers.All(n => n % 2 == 0);
    await Assert.That(allEven).IsTrue();

    var allPositive = numbers.All(n => n > 0);
    await Assert.That(allPositive).IsTrue();
}
```

Note: TUnit provides specialized collection assertions for these patterns:

```csharp
[Test]
public async Task Using_Collection_Assertions()
{
    var numbers = new[] { 2, 4, 6, 8 };

    // Instead of .All(n => n % 2 == 0):
    await Assert.That(numbers).All(n => n % 2 == 0);

    // Instead of .Any(n => n > 5):
    await Assert.That(numbers).Any(n => n > 5);
}
```

## String Boolean Methods

Many string methods return booleans:

```csharp
[Test]
public async Task String_Boolean_Methods()
{
    var text = "Hello World";

    await Assert.That(text.StartsWith("Hello")).IsTrue();
    await Assert.That(text.EndsWith("World")).IsTrue();
    await Assert.That(text.Contains("lo Wo")).IsTrue();
    await Assert.That(string.IsNullOrEmpty(text)).IsFalse();
}
```

But TUnit has more expressive string assertions:

```csharp
[Test]
public async Task Using_String_Assertions()
{
    var text = "Hello World";

    // More expressive:
    await Assert.That(text).StartsWith("Hello");
    await Assert.That(text).EndsWith("World");
    await Assert.That(text).Contains("lo Wo");
    await Assert.That(text).IsNotEmpty();
}
```

## Type Checking Booleans

```csharp
[Test]
public async Task Type_Checking()
{
    var obj = GetObject();

    await Assert.That(obj is string).IsTrue();
    await Assert.That(obj is not null).IsTrue();
}
```

Or use type assertions:

```csharp
[Test]
public async Task Using_Type_Assertions()
{
    var obj = GetObject();

    await Assert.That(obj).IsTypeOf<string>();
    await Assert.That(obj).IsNotNull();
}
```

## Common Patterns

### Toggle Testing

```csharp
[Test]
public async Task Toggle_State()
{
    var toggle = new Toggle();

    await Assert.That(toggle.IsOn).IsFalse();

    toggle.TurnOn();
    await Assert.That(toggle.IsOn).IsTrue();

    toggle.TurnOff();
    await Assert.That(toggle.IsOn).IsFalse();
}
```

### Authentication State

```csharp
[Test]
public async Task Authentication_State()
{
    var authService = new AuthenticationService();

    await Assert.That(authService.IsAuthenticated).IsFalse();

    await authService.LoginAsync("user", "password");

    await Assert.That(authService.IsAuthenticated).IsTrue();
}
```

### Validation Scenarios

```csharp
[Test]
public async Task Multiple_Validations()
{
    var form = new RegistrationForm
    {
        Email = "test@example.com",
        Password = "SecurePass123!",
        Age = 25
    };

    using (Assert.Multiple())
    {
        await Assert.That(form.IsEmailValid()).IsTrue();
        await Assert.That(form.IsPasswordStrong()).IsTrue();
        await Assert.That(form.IsAgeValid()).IsTrue();
        await Assert.That(form.IsComplete()).IsTrue();
    }
}
```

## See Also

- [Equality & Comparison](equality-and-comparison.md) - General equality testing
- [Null & Default](null-and-default.md) - Testing for null values
- [Collections](collections.md) - Collection-specific boolean tests (All, Any)
- [Type Assertions](types.md) - Type checking instead of `is` checks
