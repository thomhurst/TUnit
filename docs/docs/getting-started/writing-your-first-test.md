# Writing your first test

## Quick Start: Complete Example

Here's a complete TUnit test class with all necessary using statements:

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTestProject;

public class CalculatorTests
{
    [Test]
    public async Task Add_WithTwoNumbers_ReturnsSum()
    {
        // Arrange
        var calculator = new Calculator();

        // Act
        var result = calculator.Add(2, 3);

        // Assert
        await Assert.That(result).IsEqualTo(5);
    }
}
```

**Important**: TUnit does **not** require a `[TestClass]` attribute. Unlike MSTest or NUnit, you only need the `[Test]` attribute on your test methods.

## Step-by-Step Guide

Start by creating a new class:

```csharp
namespace MyTestProject;

public class MyTestClass
{

}
```

Now add a method with a `[Test]` attribute on it:

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    public async Task MyTest()
    {

    }
}
```

That's it. That is your runnable test.

We haven't actually made it do anything yet, but we should be able to build our project and run that test.

Tests will pass if they execute successfully without any exceptions.

## Test Method Signatures

Test methods can be either synchronous or asynchronous:

```csharp
[Test]
public void SynchronousTest()  // ✅ Valid - synchronous test
{
    var result = Calculate(2, 3);
    // Simple synchronous test without assertions
}

[Test]
public async Task AsyncTestWithAssertions()  // ✅ Recommended - asynchronous test
{
    var result = Calculate(2, 3);
    await Assert.That(result).IsEqualTo(5);  // Assertions must be awaited
}
```

**Important Notes:**
- If you use TUnit's assertion library (`Assert.That(...)`), your test **must** be `async Task` because assertions return awaitable objects that must be awaited to execute
- Synchronous `void` tests are allowed but cannot use assertions
- `async void` tests are **not allowed** and will cause a compiler error
- **Best Practice**: Use `async Task` for all tests to enable TUnit's assertion library
- **Technical Detail**: Assertions return custom assertion builder objects with a `GetAwaiter()` method, making them awaitable

Let's add some code to show you how a test might look once finished:

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    public async Task MyTest()
    {
        var result = Add(1, 2);

        await Assert.That(result).IsEqualTo(3);
    }

    private int Add(int x, int y)
    {
        return x + y;
    }
}
```

Here you can see we've executed some code and added an assertion. We'll go more into that later.

## Common Test Patterns

### Testing Boolean Returns

When testing methods that return boolean values, use `IsTrue()` or `IsFalse()`:

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTestProject;

public class ValidatorTests
{
    [Test]
    public async Task IsPositive_WithNegativeNumber_ReturnsFalse()
    {
        // Arrange & Act
        var result = Validator.IsPositive(-1);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsPositive_WithPositiveNumber_ReturnsTrue()
    {
        // Arrange & Act
        var result = Validator.IsPositive(5);

        // Assert
        await Assert.That(result).IsTrue();
    }
}

public static class Validator
{
    public static bool IsPositive(int number)
    {
        return number > 0;
    }
}
```

### Testing with Multiple Assertions

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTestProject;

public class StringTests
{
    [Test]
    public async Task ProcessString_WithValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var input = "hello";

        // Act
        var result = input.ToUpper();

        // Assert
        await Assert.That(result).IsEqualTo("HELLO");
        await Assert.That(result).HasLength(5);
        await Assert.That(result.StartsWith("HE")).IsTrue();
    }
}
```

### Using Statements

The examples above show explicit using statements for clarity:

```csharp
using TUnit.Core;                    // For [Test] attribute
using TUnit.Assertions;              // For Assert.That()
using TUnit.Assertions.Extensions;   // For assertion methods like IsEqualTo(), IsTrue(), etc.
```

**However**, the TUnit package automatically configures these namespaces as global usings, so in practice you don't need to include them in each test file. Your test classes can be as simple as:

```csharp
namespace MyTestProject;

public class ValidatorTests
{
    [Test]
    public async Task IsPositive_WithNegativeNumber_ReturnsFalse()
    {
        var result = Validator.IsPositive(-1);
        await Assert.That(result).IsFalse();
    }
}
``` 
