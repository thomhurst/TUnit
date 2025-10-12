---
sidebar_position: 2
---

# Source Generator Assertions

TUnit provides source generators to make creating custom assertions incredibly easy. Instead of manually writing assertion classes and extension methods, you can simply decorate your methods with attributes and let the generator do the work.

## Overview

There are two ways to create assertions with source generators:

1. **`[GenerateAssertion]`** - Decorate your own methods to generate assertions
2. **`[AssertionFrom<T>]`** - Generate assertions from existing library methods

---

## Method-Level Generation: `[GenerateAssertion]`

The `[GenerateAssertion]` attribute allows you to turn any method into a full assertion with minimal code.

### Basic Example

```csharp
using System.ComponentModel;
using TUnit.Assertions.Attributes;

public static partial class IntAssertionExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion]
    public static bool IsPositive(this int value)
    {
        return value > 0;
    }
}

// Usage in tests:
await Assert.That(5).IsPositive();  // ✅ Passes
await Assert.That(-3).IsPositive(); // ❌ Fails with clear message
```

**Note:** The `[EditorBrowsable(EditorBrowsableState.Never)]` attribute hides the helper method from IntelliSense. Users will only see the generated assertion extension method `IsPositive()` on `Assert.That(...)`, not the underlying helper method on `int` values.

### What Gets Generated

The generator creates:
1. An `Assertion<T>` class containing your logic
2. An extension method on `IAssertionSource<T>`
3. Full support for chaining with `.And` and `.Or`

```csharp
// Generated code (simplified):
public sealed class IsPositive_Assertion : Assertion<int>
{
    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<int> metadata)
    {
        var result = metadata.Value.IsPositive();
        return result ? AssertionResult.Passed : AssertionResult.Failed($"found {metadata.Value}");
    }

    protected override string GetExpectation() => "to satisfy IsPositive";
}

public static IsPositive_Assertion IsPositive(this IAssertionSource<int> source)
{
    source.Context.ExpressionBuilder.Append(".IsPositive()");
    return new IsPositive_Assertion(source.Context);
}
```

---

## Supported Return Types

### 1. `bool` - Simple Pass/Fail

```csharp
[GenerateAssertion]
public static bool IsEven(this int value)
{
    return value % 2 == 0;
}

// Usage:
await Assert.That(4).IsEven();
```

### 2. `AssertionResult` - Custom Messages

When you need more control over error messages, return `AssertionResult`:

```csharp
[GenerateAssertion]
public static AssertionResult IsPrime(this int value)
{
    if (value < 2)
        return AssertionResult.Failed($"{value} is less than 2");

    for (int i = 2; i <= Math.Sqrt(value); i++)
    {
        if (value % i == 0)
            return AssertionResult.Failed($"{value} is divisible by {i}");
    }

    return AssertionResult.Passed;
}

// Usage:
await Assert.That(17).IsPrime();
// If fails: "Expected to satisfy IsPrime but 15 is divisible by 3"
```

### 3. `Task<bool>` - Async Operations

```csharp
[GenerateAssertion]
public static async Task<bool> ExistsInDatabaseAsync(this int userId, DbContext db)
{
    return await db.Users.AnyAsync(u => u.Id == userId);
}

// Usage:
await Assert.That(userId).ExistsInDatabaseAsync(dbContext);
```

### 4. `Task<AssertionResult>` - Async with Custom Messages

```csharp
[GenerateAssertion]
public static async Task<AssertionResult> HasValidEmailAsync(this int userId, DbContext db)
{
    var user = await db.Users.FindAsync(userId);

    if (user == null)
        return AssertionResult.Failed($"User {userId} not found");

    if (!user.Email.Contains("@"))
        return AssertionResult.Failed($"Email '{user.Email}' is invalid");

    return AssertionResult.Passed;
}

// Usage:
await Assert.That(123).HasValidEmailAsync(dbContext);
```

---

## Methods with Parameters

Add parameters to make your assertions flexible:

```csharp
[GenerateAssertion]
public static bool IsGreaterThan(this int value, int threshold)
{
    return value > threshold;
}

[GenerateAssertion]
public static bool IsBetween(this int value, int min, int max)
{
    return value >= min && value <= max;
}

// Usage:
await Assert.That(10).IsGreaterThan(5);
await Assert.That(7).IsBetween(1, 10);
```

**Benefits:**
- Parameters automatically get `[CallerArgumentExpression]` for great error messages
- Each parameter becomes part of the extension method signature
- Error messages show actual values: `"Expected to satisfy IsGreaterThan(5) but found 3"`

---

## Class-Level Generation: `[AssertionFrom]`

Use `[AssertionFrom]` to create assertions from existing methods in libraries or your codebase.

### Basic Usage

```csharp
using TUnit.Assertions.Attributes;

[AssertionFrom<string>("IsNullOrEmpty")]
[AssertionFrom<string>("StartsWith")]
[AssertionFrom<string>("EndsWith")]
public static partial class StringAssertionExtensions
{
}

// Usage:
await Assert.That(myString).IsNullOrEmpty();
await Assert.That("hello").StartsWith("he");
```

### With Custom Names

```csharp
[AssertionFrom<string>("Contains", CustomName = "Has")]
public static partial class StringAssertionExtensions
{
}

// Usage:
await Assert.That("hello world").Has("world");
```

### Negation Support

For `bool`-returning methods, you can generate negated versions:

```csharp
[AssertionFrom<string>("Contains", CustomName = "DoesNotContain", NegateLogic = true)]
public static partial class StringAssertionExtensions
{
}

// Usage:
await Assert.That("hello").DoesNotContain("xyz");
```

**Note:** Negation only works with `bool`-returning methods. `AssertionResult` methods determine their own pass/fail logic.

### Referencing Methods on Different Types

```csharp
// Reference static methods from another type
[AssertionFrom<string>(typeof(StringHelper), "IsValidEmail")]
public static partial class StringAssertionExtensions
{
}

// Where StringHelper is:
public static class StringHelper
{
    public static bool IsValidEmail(string value)
    {
        return value.Contains("@");
    }
}
```

---

## Requirements and Best Practices

### Method Requirements

For `[GenerateAssertion]`, your method must:
- Be `static`
- Have at least one parameter (the value to assert)
- Return `bool`, `AssertionResult`, `Task<bool>`, or `Task<AssertionResult>`

### Hiding Helper Methods from IntelliSense

**Important:** Always use `[EditorBrowsable(EditorBrowsableState.Never)]` on your `[GenerateAssertion]` methods to prevent IntelliSense pollution.

```csharp
using System.ComponentModel;
using TUnit.Assertions.Attributes;

public static partial class StringAssertionExtensions
{
    // ✅ GOOD: Hidden from IntelliSense
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion]
    public static bool IsEmptyString(this string value) => value.Length == 0;

    // ❌ BAD: Will appear in IntelliSense when typing on string values
    [GenerateAssertion]
    public static bool IsEmptyString(this string value) => value.Length == 0;
}
```

**Why?** Without `[EditorBrowsable]`, the helper method appears in IntelliSense when users type on the actual type (e.g., `myString.`). With the attribute, users only see the proper assertion method on `Assert.That(myString).`, which is cleaner and less confusing.

### Recommended Patterns

✅ **DO:**
- **Always** use `[EditorBrowsable(EditorBrowsableState.Never)]` on `[GenerateAssertion]` methods
- Use extension methods for cleaner syntax
- Return `AssertionResult` when you need custom error messages
- Use async when performing I/O or database operations
- Keep assertion logic simple and focused
- Use descriptive method names

❌ **DON'T:**
- Put complex business logic in assertions
- Make assertions with side effects
- Use `AssertionResult` with negation (it won't work as expected)
- Forget to make the containing class `partial`
- Skip the `[EditorBrowsable]` attribute (causes IntelliSense clutter)

---

## Chaining and Composition

All generated assertions support chaining:

```csharp
[GenerateAssertion]
public static bool IsPositive(this int value) => value > 0;

[GenerateAssertion]
public static bool IsEven(this int value) => value % 2 == 0;

// Usage:
await Assert.That(10)
    .IsPositive()
    .And.IsEven();

// Or:
await Assert.That(number)
    .IsEven()
    .Or.IsPositive();
```

---

## Migration from CreateAssertion

If you're using the old `CreateAssertionAttribute`:

```csharp
// Old (still works, but deprecated):
[CreateAssertion<string>("StartsWith")]
public static partial class StringAssertionExtensions { }

// New:
[AssertionFrom<string>("StartsWith")]
public static partial class StringAssertionExtensions { }
```

The old attribute shows an obsolete warning but continues to work for backward compatibility.

---

## Complete Example

Here's a comprehensive example showing all features:

```csharp
using System.ComponentModel;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

public static partial class UserAssertionExtensions
{
    // Simple bool
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion]
    public static bool HasValidId(this User user)
    {
        return user.Id > 0;
    }

    // With parameters
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion]
    public static bool HasRole(this User user, string role)
    {
        return user.Roles.Contains(role);
    }

    // Custom messages with AssertionResult
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion]
    public static AssertionResult HasValidEmail(this User user)
    {
        if (string.IsNullOrEmpty(user.Email))
            return AssertionResult.Failed("Email is null or empty");

        if (!user.Email.Contains("@"))
            return AssertionResult.Failed($"Email '{user.Email}' is not valid");

        return AssertionResult.Passed;
    }

    // Async with database
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion]
    public static async Task<bool> ExistsInDatabaseAsync(this User user, DbContext db)
    {
        return await db.Users.AnyAsync(u => u.Id == user.Id);
    }
}

// Usage in tests:
[Test]
public async Task ValidateUser()
{
    var user = new User { Id = 1, Email = "test@example.com", Roles = ["Admin"] };

    await Assert.That(user).HasValidId();
    await Assert.That(user).HasRole("Admin");
    await Assert.That(user).HasValidEmail();
    await Assert.That(user).ExistsInDatabaseAsync(dbContext);

    // Chaining:
    await Assert.That(user)
        .HasValidId()
        .And.HasValidEmail()
        .And.HasRole("Admin");
}
```

---

## See Also

- [Custom Assertions (Manual)](./custom-assertions.md) - For when you need full control
