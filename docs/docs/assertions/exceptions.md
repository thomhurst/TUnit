---
sidebar_position: 8
---

# Exception Assertions

TUnit provides comprehensive assertions for testing that code throws (or doesn't throw) exceptions, with rich support for validating exception types, messages, and properties.

## Basic Exception Assertions

### Throws&lt;TException&gt;

Tests that a delegate throws a specific exception type (or a subclass):

```csharp
[Test]
public async Task Code_Throws_Exception()
{
    await Assert.That(() => int.Parse("not a number"))
        .Throws<FormatException>();
}
```

Works with any exception type:

```csharp
[Test]
public async Task Various_Exception_Types()
{
    await Assert.That(() => throw new InvalidOperationException())
        .Throws<InvalidOperationException>();

    await Assert.That(() => throw new ArgumentNullException())
        .Throws<ArgumentNullException>();

    await Assert.That(() => File.ReadAllText("nonexistent.txt"))
        .Throws<FileNotFoundException>();
}
```

### ThrowsExactly&lt;TException&gt;

Tests that a delegate throws the exact exception type (not a subclass):

```csharp
[Test]
public async Task Throws_Exact_Type()
{
    await Assert.That(() => throw new ArgumentNullException())
        .ThrowsExactly<ArgumentNullException>();

    // This would fail - ArgumentNullException is a subclass of ArgumentException
    // await Assert.That(() => throw new ArgumentNullException())
    //     .ThrowsExactly<ArgumentException>();
}
```

### Throws (Runtime Type)

Use when the exception type is only known at runtime:

```csharp
[Test]
public async Task Throws_Runtime_Type()
{
    Type exceptionType = typeof(InvalidOperationException);

    await Assert.That(() => throw new InvalidOperationException())
        .Throws(exceptionType);
}
```

### ThrowsNothing

Tests that code does not throw any exception:

```csharp
[Test]
public async Task Code_Does_Not_Throw()
{
    await Assert.That(() => int.Parse("42"))
        .ThrowsNothing();

    await Assert.That(() => ValidateInput("valid"))
        .ThrowsNothing();
}
```

## Async Exception Assertions

For async operations, use async delegates:

```csharp
[Test]
public async Task Async_Throws_Exception()
{
    await Assert.That(async () => await FailingOperationAsync())
        .Throws<HttpRequestException>();
}
```

```csharp
[Test]
public async Task Async_Does_Not_Throw()
{
    await Assert.That(async () => await SuccessfulOperationAsync())
        .ThrowsNothing();
}
```

## Exception Message Assertions

### WithMessage

Tests that the exception has an exact message:

```csharp
[Test]
public async Task Exception_With_Exact_Message()
{
    await Assert.That(() => throw new InvalidOperationException("Operation failed"))
        .Throws<InvalidOperationException>()
        .WithMessage("Operation failed");
}
```

### WithMessageContaining

Tests that the exception message contains a substring:

```csharp
[Test]
public async Task Exception_Message_Contains()
{
    await Assert.That(() => throw new ArgumentException("The parameter 'userId' is invalid"))
        .Throws<ArgumentException>()
        .WithMessageContaining("userId");
}
```

#### Case-Insensitive

```csharp
[Test]
public async Task Message_Contains_Ignoring_Case()
{
    await Assert.That(() => throw new Exception("ERROR: Failed"))
        .Throws<Exception>()
        .WithMessageContaining("error")
        .IgnoringCase();
}
```

### WithMessageNotContaining

Tests that the exception message does not contain a substring:

```csharp
[Test]
public async Task Message_Does_Not_Contain()
{
    await Assert.That(() => throw new Exception("User error"))
        .Throws<Exception>()
        .WithMessageNotContaining("system");
}
```

### WithMessageMatching

Tests that the exception message matches a pattern:

```csharp
[Test]
public async Task Message_Matches_Pattern()
{
    await Assert.That(() => throw new Exception("Error code: 12345"))
        .Throws<Exception>()
        .WithMessageMatching("Error code: *");
}
```

Or with a `StringMatcher`:

```csharp
[Test]
public async Task Message_Matches_With_Matcher()
{
    var matcher = new StringMatcher("Error * occurred", caseSensitive: false);

    await Assert.That(() => throw new Exception("Error 500 occurred"))
        .Throws<Exception>()
        .WithMessageMatching(matcher);
}
```

## ArgumentException Specific

### WithParameterName

For `ArgumentException` and its subclasses, you can assert on the parameter name:

```csharp
[Test]
public async Task ArgumentException_With_Parameter_Name()
{
    await Assert.That(() => ValidateUser(null!))
        .Throws<ArgumentNullException>()
        .WithParameterName("user");
}

void ValidateUser(User user)
{
    if (user == null)
        throw new ArgumentNullException(nameof(user));
}
```

Combine with message assertions:

```csharp
[Test]
public async Task ArgumentException_Parameter_And_Message()
{
    await Assert.That(() => SetAge(-1))
        .Throws<ArgumentOutOfRangeException>()
        .WithParameterName("age")
        .WithMessageContaining("must be positive");
}

void SetAge(int age)
{
    if (age < 0)
        throw new ArgumentOutOfRangeException(nameof(age), "Age must be positive");
}
```

## Inner Exception Assertions

### WithInnerException

Assert on the inner exception:

```csharp
[Test]
public async Task Exception_With_Inner_Exception()
{
    await Assert.That(() => {
        try
        {
            int.Parse("not a number");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Processing failed", ex);
        }
    })
    .Throws<InvalidOperationException>()
    .WithInnerException();
}
```

Chain to assert on the inner exception type:

```csharp
[Test]
public async Task Inner_Exception_Type()
{
    await Assert.That(() => ThrowWithInner())
        .Throws<InvalidOperationException>()
        .WithInnerException()
        .Throws<FormatException>();
}

void ThrowWithInner()
{
    try
    {
        int.Parse("abc");
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Outer", ex);
    }
}
```

## Practical Examples

### Validation Exceptions

```csharp
[Test]
public async Task Validate_Email_Throws()
{
    await Assert.That(() => ValidateEmail("invalid-email"))
        .Throws<ArgumentException>()
        .WithParameterName("email")
        .WithMessageContaining("valid email");
}
```

### Null Argument Checks

```csharp
[Test]
public async Task Null_Argument_Throws()
{
    await Assert.That(() => ProcessData(null!))
        .Throws<ArgumentNullException>()
        .WithParameterName("data");
}
```

### File Operations

```csharp
[Test]
public async Task File_Not_Found()
{
    await Assert.That(() => File.ReadAllText("nonexistent.txt"))
        .Throws<FileNotFoundException>()
        .WithMessageContaining("nonexistent.txt");
}
```

### Network Operations

```csharp
[Test]
public async Task HTTP_Request_Fails()
{
    await Assert.That(async () => await _client.GetAsync("http://invalid-url"))
        .Throws<HttpRequestException>();
}
```

### Database Operations

```csharp
[Test]
public async Task Duplicate_Key_Violation()
{
    await Assert.That(async () => await InsertDuplicateAsync())
        .Throws<DbUpdateException>()
        .WithMessageContaining("duplicate key");
}
```

### Division by Zero

```csharp
[Test]
public async Task Division_By_Zero()
{
    await Assert.That(() => {
        int a = 10;
        int b = 0;
        return a / b;
    })
    .Throws<DivideByZeroException>();
}
```

### Index Out of Range

```csharp
[Test]
public async Task Array_Index_Out_Of_Range()
{
    var array = new[] { 1, 2, 3 };

    await Assert.That(() => array[10])
        .Throws<IndexOutOfRangeException>();
}
```

### Invalid Cast

```csharp
[Test]
public async Task Invalid_Cast()
{
    object obj = "string";

    await Assert.That(() => (int)obj)
        .Throws<InvalidCastException>();
}
```

### Custom Exceptions

```csharp
public class BusinessRuleException : Exception
{
    public string RuleCode { get; }

    public BusinessRuleException(string ruleCode, string message)
        : base(message)
    {
        RuleCode = ruleCode;
    }
}

[Test]
public async Task Custom_Exception_With_Properties()
{
    var exception = await Assert.That(() =>
        throw new BusinessRuleException("BR001", "Business rule violated"))
        .Throws<BusinessRuleException>();

    // Can't directly assert on exception properties yet, but you can access them
    await Assert.That(exception.RuleCode).IsEqualTo("BR001");
    await Assert.That(exception.Message).Contains("Business rule");
}
```

## Testing Multiple Operations

### Using Assert.Multiple

```csharp
[Test]
public async Task Multiple_Exception_Scenarios()
{
    using (Assert.Multiple())
    {
        await Assert.That(() => int.Parse("abc"))
            .Throws<FormatException>();

        await Assert.That(() => int.Parse("999999999999999999999"))
            .Throws<OverflowException>();

        await Assert.That(() => int.Parse("42"))
            .ThrowsNothing();
    }
}
```

## Exception Inheritance

When using `Throws<T>()`, subclasses are accepted:

```csharp
[Test]
public async Task Exception_Inheritance()
{
    // ArgumentNullException inherits from ArgumentException
    await Assert.That(() => throw new ArgumentNullException())
        .Throws<ArgumentException>(); // ✅ Passes

    await Assert.That(() => throw new ArgumentNullException())
        .Throws<ArgumentNullException>(); // ✅ Also passes
}
```

Use `ThrowsExactly<T>()` if you need the exact type:

```csharp
[Test]
public async Task Exact_Exception_Type()
{
    // This fails - ArgumentNullException is not exactly ArgumentException
    // await Assert.That(() => throw new ArgumentNullException())
    //     .ThrowsExactly<ArgumentException>();

    await Assert.That(() => throw new ArgumentException())
        .ThrowsExactly<ArgumentException>(); // ✅ Passes
}
```

## Aggregate Exceptions

```csharp
[Test]
public async Task Aggregate_Exception()
{
    await Assert.That(() => {
        var task1 = Task.Run(() => throw new InvalidOperationException());
        var task2 = Task.Run(() => throw new ArgumentException());
        Task.WaitAll(task1, task2);
    })
    .Throws<AggregateException>();
}
```

## Chaining Exception Assertions

```csharp
[Test]
public async Task Chained_Exception_Assertions()
{
    await Assert.That(() => ValidateInput(""))
        .Throws<ArgumentException>()
        .WithParameterName("input")
        .WithMessageContaining("cannot be empty")
        .WithMessageNotContaining("null");
}
```

## Testing that No Exception is Thrown

### ThrowsNothing vs Try-Catch

```csharp
[Test]
public async Task Explicit_No_Exception()
{
    // Using ThrowsNothing
    await Assert.That(() => SafeOperation())
        .ThrowsNothing();

    // Alternative: just call it
    SafeOperation(); // If it throws, the test fails
}
```

## Common Patterns

### Expected Failures

```csharp
[Test]
public async Task Expected_Validation_Failure()
{
    var invalidUser = new User { Age = -1 };

    await Assert.That(() => ValidateUser(invalidUser))
        .Throws<ValidationException>()
        .WithMessageContaining("Age");
}
```

### Defensive Programming

```csharp
[Test]
public async Task Guard_Clause_Validation()
{
    await Assert.That(() => new Service(null!))
        .Throws<ArgumentNullException>()
        .WithParameterName("dependency");
}
```

### State Validation

```csharp
[Test]
public async Task Invalid_State_Operation()
{
    var connection = new Connection();
    // Don't connect

    await Assert.That(() => connection.SendData("test"))
        .Throws<InvalidOperationException>()
        .WithMessageContaining("not connected");
}
```

### Configuration Errors

```csharp
[Test]
public async Task Missing_Configuration()
{
    await Assert.That(() => LoadConfiguration("invalid.json"))
        .Throws<ConfigurationException>()
        .WithMessageContaining("invalid.json");
}
```

## Timeout Exceptions

```csharp
[Test]
public async Task Operation_Timeout()
{
    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

    await Assert.That(async () => await LongRunningOperationAsync(cts.Token))
        .Throws<TaskCanceledException>();
}
```

## Re-throwing Exceptions

```csharp
[Test]
public async Task Wrapper_Exception()
{
    await Assert.That(() => {
        try
        {
            RiskyOperation();
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Operation failed", ex);
        }
    })
    .Throws<ApplicationException>()
    .WithInnerException();
}
```

## Exception Assertions with Async/Await

```csharp
[Test]
public async Task Async_Exception_Handling()
{
    await Assert.That(async () => {
        await Task.Delay(10);
        throw new InvalidOperationException("Async failure");
    })
    .Throws<InvalidOperationException>()
    .WithMessageContaining("Async failure");
}
```

## See Also

- [Tasks & Async](tasks-and-async.md) - Testing async operations and task state
- [Types](types.md) - Type checking for exception types
- [Strings](string.md) - String assertions for exception messages
