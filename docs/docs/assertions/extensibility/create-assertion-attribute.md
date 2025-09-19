---
sidebar_position: 4
---

# CreateAssertion Attribute

The `[CreateAssertion<T>]` attribute provides a powerful way to automatically generate assertion extension methods from existing methods that return boolean values. This approach eliminates boilerplate code and ensures consistency across your assertion library.

## Overview

The `[CreateAssertion<T>]` attribute is a source generator feature that:
- Automatically creates assertion extension methods from existing boolean-returning methods
- Supports both instance and static methods
- Can generate both positive and negative assertions
- Maintains full IntelliSense support and compile-time safety

## Basic Usage

### Instance Methods

For methods that exist on the type being asserted:

```csharp
using TUnit.Assertions.Attributes;

[CreateAssertion<string>(nameof(string.StartsWith))]
[CreateAssertion<string>(nameof(string.EndsWith))]
[CreateAssertion<string>(nameof(string.Contains))]
public static partial class StringAssertionExtensions;
```

This generates assertion methods that can be used as:

```csharp
await Assert.That("Hello World").StartsWith("Hello");
await Assert.That("Hello World").EndsWith("World");
await Assert.That("Hello World").Contains("lo Wo");
```

### Static Methods

For static methods that take the asserted type as a parameter:

```csharp
using System.IO;
using TUnit.Assertions.Attributes;

[CreateAssertion<string>(typeof(Path), nameof(Path.IsPathRooted), CustomName = "IsRootedPath")]
public static partial class PathAssertionExtensions;
```

Usage:
```csharp
await Assert.That(@"C:\Users\Documents").IsRootedPath();
```

## Advanced Features

### Custom Names

You can specify custom names for generated methods using the `CustomName` property:

```csharp
[CreateAssertion<char>(nameof(char.IsDigit))]
[CreateAssertion<char>(nameof(char.IsDigit), CustomName = "IsNumeric")]  // Alias
public static partial class CharAssertionExtensions;
```

### Negative Assertions

Generate negative assertions by setting `NegateLogic = true`:

```csharp
[CreateAssertion<string>(nameof(string.Contains))]
[CreateAssertion<string>(nameof(string.Contains), CustomName = "DoesNotContain", NegateLogic = true)]
public static partial class StringAssertionExtensions;
```

Usage:
```csharp
await Assert.That("Hello").Contains("ell");        // Passes
await Assert.That("Hello").DoesNotContain("xyz");  // Passes
```

### Multiple Assertions on One Class

You can apply multiple attributes to generate a comprehensive set of assertions:

```csharp
[CreateAssertion<char>(nameof(char.IsDigit))]
[CreateAssertion<char>(nameof(char.IsDigit), CustomName = "IsNotDigit", NegateLogic = true)]
[CreateAssertion<char>(nameof(char.IsLetter))]
[CreateAssertion<char>(nameof(char.IsLetter), CustomName = "IsNotLetter", NegateLogic = true)]
[CreateAssertion<char>(nameof(char.IsLetterOrDigit))]
[CreateAssertion<char>(nameof(char.IsLetterOrDigit), CustomName = "IsNotLetterOrDigit", NegateLogic = true)]
[CreateAssertion<char>(nameof(char.IsWhiteSpace))]
[CreateAssertion<char>(nameof(char.IsWhiteSpace), CustomName = "IsNotWhiteSpace", NegateLogic = true)]
public static partial class CharAssertionExtensions;
```

## Attribute Properties

### Constructor Parameters

1. **Single Parameter Constructor** - For instance methods on the target type:
   ```csharp
   [CreateAssertion<TTarget>(string methodName)]
   ```

2. **Two Parameter Constructor** - For static methods on a different type:
   ```csharp
   [CreateAssertion<TTarget>(Type containingType, string methodName)]
   ```

### Optional Properties

- **`CustomName`**: Override the generated method name
- **`NegateLogic`**: Invert the boolean result for negative assertions
- **`RequiresGenericTypeParameter`**: For methods that need generic type handling
- **`TreatAsInstance`**: Force treating a static method as instance (useful for extension methods)

## Complete Examples

### Example 1: DateTime Assertions

```csharp
using System;
using TUnit.Assertions.Attributes;

[CreateAssertion<DateTime>(nameof(DateTime.IsLeapYear), CustomName = "IsInLeapYear")]
[CreateAssertion<DateTime>(nameof(DateTime.IsLeapYear), CustomName = "IsNotInLeapYear", NegateLogic = true)]
[CreateAssertion<DateTime>(nameof(DateTime.IsDaylightSavingTime))]
[CreateAssertion<DateTime>(nameof(DateTime.IsDaylightSavingTime), CustomName = "IsNotDaylightSavingTime", NegateLogic = true)]
public static partial class DateTimeAssertionExtensions;
```

### Example 2: File System Assertions

```csharp
using System.IO;
using TUnit.Assertions.Attributes;

[CreateAssertion<FileInfo>(nameof(FileInfo.Exists))]
[CreateAssertion<FileInfo>(nameof(FileInfo.Exists), CustomName = "DoesNotExist", NegateLogic = true)]
[CreateAssertion<FileInfo>(nameof(FileInfo.IsReadOnly))]
[CreateAssertion<FileInfo>(nameof(FileInfo.IsReadOnly), CustomName = "IsNotReadOnly", NegateLogic = true)]
public static partial class FileInfoAssertionExtensions;
```

Usage:
```csharp
var file = new FileInfo(@"C:\temp\test.txt");
await Assert.That(file).Exists();
await Assert.That(file).IsNotReadOnly();
```

### Example 3: Custom Type Assertions

```csharp
// Your custom type
public class User
{
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public bool HasPremiumAccess() => /* logic */;
}

// Assertion extensions
[CreateAssertion<User>(nameof(User.IsActive))]
[CreateAssertion<User>(nameof(User.IsActive), CustomName = "IsInactive", NegateLogic = true)]
[CreateAssertion<User>(nameof(User.IsVerified))]
[CreateAssertion<User>(nameof(User.HasPremiumAccess))]
public static partial class UserAssertionExtensions;

// Usage
var user = new User { IsActive = true, IsVerified = false };
await Assert.That(user).IsActive();
await Assert.That(user).IsNotVerified();
```

## Benefits

1. **Reduced Boilerplate**: No need to write repetitive assertion methods
2. **Consistency**: All generated assertions follow the same pattern
3. **Type Safety**: Full compile-time checking and IntelliSense support
4. **Maintainability**: Changes to the source method signature are automatically reflected
5. **Performance**: Source-generated code has no runtime overhead

## Requirements

- The target method must return a `bool`
- The containing class must be `partial`
- The containing class must be `static` for extension methods
- The method parameters must be compatible with the assertion pattern

## Migration from Manual Assertions

If you have existing manual assertion methods, you can gradually migrate to using `[CreateAssertion<T>]`:

```csharp
// Before - Manual implementation
public static InvokableValueAssertionBuilder<string> StartsWith(
    this IValueSource<string> valueSource, 
    string expected)
{
    return valueSource.RegisterAssertion(
        new StringStartsWithCondition(expected),
        [expected]);
}

// After - Using CreateAssertion
[CreateAssertion<string>(nameof(string.StartsWith))]
public static partial class StringAssertionExtensions;
```

## Best Practices

1. **Group Related Assertions**: Keep assertions for similar types in the same partial class
2. **Consistent Naming**: Use `CustomName` to maintain consistent naming patterns
3. **Provide Both Positive and Negative**: Where it makes sense, provide both forms
4. **Document Complex Cases**: Add XML documentation comments to the partial class for complex scenarios
5. **Test Generated Code**: Ensure generated assertions behave as expected

## Limitations

- Only works with methods that return `bool`
- Cannot handle methods with complex parameter patterns
- Generic constraints on the method itself may require manual implementation
- Methods with optional parameters may need special handling

For cases that can't be handled by `[CreateAssertion<T>]`, you can still write manual assertion methods alongside the generated ones in the same partial class.