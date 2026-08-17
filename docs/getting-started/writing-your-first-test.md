# Writing your first test

## Quick Start: Complete Example[​](#quick-start-complete-example "Direct link to Quick Start: Complete Example")

Here's a complete TUnit test class with all necessary using statements:

```
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

## Step-by-Step Guide[​](#step-by-step-guide "Direct link to Step-by-Step Guide")

Auto-Imported Namespaces

The TUnit package automatically configures global usings for `TUnit.Core`, `TUnit.Assertions`, and `TUnit.Assertions.Extensions`. The explicit `using` statements in the examples below are shown for clarity — you don't need them in practice.

Start by creating a new class:

```
namespace MyTestProject;



public class MyTestClass

{



}
```

Now add a method with a `[Test]` attribute on it:

```
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

## Test Method Signatures[​](#test-method-signatures "Direct link to Test Method Signatures")

Test methods can be either synchronous or asynchronous:

```
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

* If you use `Assert.That(...)`, your test **must** be `async Task` — assertions return awaitable objects that won't execute without `await`
* Synchronous `void` tests are allowed but cannot use assertions
* `async void` tests are **not allowed** — the TUnit analyzers report this as a build error (diagnostic `TUnit0031`)

See [Awaiting Assertions](/docs/assertions/awaiting.md) for more details.

Let's add some code to show you how a test might look once finished:

```
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

## Common Test Patterns[​](#common-test-patterns "Direct link to Common Test Patterns")

### Testing Boolean Returns[​](#testing-boolean-returns "Direct link to Testing Boolean Returns")

When testing methods that return boolean values, use `IsTrue()` or `IsFalse()`:

```
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

### Testing with Multiple Assertions[​](#testing-with-multiple-assertions "Direct link to Testing with Multiple Assertions")

```
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

        await Assert.That(result).Length().IsEqualTo(5);

        await Assert.That(result.StartsWith("HE")).IsTrue();

    }

}
```

**Next:** [Run Your Tests →](/docs/getting-started/running-your-tests.md)
