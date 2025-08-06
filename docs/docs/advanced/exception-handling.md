# Exception Handling

TUnit provides a comprehensive exception hierarchy to help you understand and handle different types of failures that can occur during test execution. Understanding these exceptions is crucial for debugging test failures and implementing custom error handling.

## Exception Hierarchy

All TUnit-specific exceptions inherit from `TUnitException`, which extends `Exception`. This allows you to catch all TUnit-related exceptions with a single catch block if needed.

```
Exception
└── TUnitException
    ├── Hook Exceptions
    │   ├── BeforeTestException
    │   ├── AfterTestException
    │   ├── BeforeClassException
    │   ├── AfterClassException
    │   ├── BeforeAssemblyException
    │   ├── AfterAssemblyException
    │   ├── BeforeTestSessionException
    │   └── AfterTestSessionException
    ├── Framework Exceptions
    │   ├── TestFailedInitializationException
    │   ├── DependencyConflictException
    │   ├── InconclusiveTestException
    │   └── TestNotFoundException
    └── Execution Exceptions
        ├── TimeoutException
        ├── SkipTestException
        └── TestExecutionException
```

## Common Exception Types

### Hook Exceptions

Hook exceptions are thrown when setup or cleanup operations fail. Each hook type has its own exception to help identify where in the test lifecycle the failure occurred.

#### BeforeTestException / AfterTestException

Thrown when a `[Before(HookType.Test)]` or `[After(HookType.Test)]` hook fails.

```csharp
[Before(HookType.Test)]
public async Task TestSetup()
{
    // If this throws, it will be wrapped in BeforeTestException
    await DatabaseConnection.OpenAsync();
}

[Test]
public async Task MyTest()
{
    try
    {
        // Test code
    }
    catch (BeforeTestException ex)
    {
        // The setup failed
        _logger.LogError($"Test setup failed: {ex.InnerException?.Message}");
        throw;
    }
}
```

#### BeforeClassException / AfterClassException

Thrown when class-level hooks fail. These affect all tests in the class.

```csharp
[Before(HookType.Class)]
public static async Task ClassSetup()
{
    // If this fails, all tests in the class will be marked as failed
    // with a BeforeClassException
}
```

### Framework Exceptions

These exceptions indicate problems with test configuration or framework usage.

#### TestFailedInitializationException

Thrown when a test cannot be initialized properly, often due to constructor failures or missing dependencies.

```csharp
public class MyTests
{
    private readonly IService _service;
    
    public MyTests(IService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        // If DI fails to provide service, TestFailedInitializationException is thrown
    }
}
```

#### DependencyConflictException

Thrown when there are circular dependencies or conflicting test dependencies.

```csharp
[Test]
[DependsOn(nameof(Test2))]
public void Test1() { }

[Test]
[DependsOn(nameof(Test1))] // Circular dependency!
public void Test2() { }
// Results in DependencyConflictException
```

#### InconclusiveTestException

Used to mark a test as inconclusive when it cannot determine pass/fail status.

```csharp
[Test]
public async Task CheckExternalService()
{
    var service = await GetExternalServiceStatus();
    
    if (service.IsInMaintenanceMode)
    {
        throw new InconclusiveTestException(
            "Cannot test: service is in maintenance mode");
    }
    
    // Continue with actual test
}
```

### Execution Exceptions

These exceptions relate to test execution behavior.

#### TimeoutException

Thrown when a test exceeds its timeout limit.

```csharp
[Test]
[Timeout(1000)] // 1 second timeout
public async Task LongRunningTest()
{
    await Task.Delay(2000); // Will throw TimeoutException
}
```

#### SkipTestException

Used to dynamically skip a test during execution.

```csharp
[Test]
public async Task ConditionalTest()
{
    if (!await CheckPreconditions())
    {
        throw new SkipTestException("Preconditions not met");
    }
    
    // Test logic
}
```

## Exception Properties and Information

TUnit exceptions provide rich information to help diagnose issues:

### Stack Trace Enhancement

Many TUnit exceptions enhance stack traces to provide more relevant information:

```csharp
public class TUnitException : Exception
{
    public override string? StackTrace => 
        EnhancedStackTrace ?? base.StackTrace;
        
    protected string? EnhancedStackTrace { get; set; }
}
```

### Context Information

Exceptions often include test context information:

```csharp
catch (BeforeTestException ex)
{
    Console.WriteLine($"Hook failed for test: {ex.TestContext.TestName}");
    Console.WriteLine($"In class: {ex.TestContext.TestClass.FullName}");
    Console.WriteLine($"Hook method: {ex.HookMethod.Name}");
}
```

## Handling Exceptions in Custom Extensions

When implementing custom test executors or hook executors, proper exception handling is crucial:

### Test Executor Exception Handling

```csharp
public class SafeTestExecutor : ITestExecutor
{
    public async Task ExecuteAsync(TestContext context, Func<Task> testBody)
    {
        try
        {
            await testBody();
        }
        catch (TUnitException)
        {
            // TUnit exceptions should generally be rethrown
            throw;
        }
        catch (AssertionException)
        {
            // Assertion failures should be rethrown
            throw;
        }
        catch (Exception ex)
        {
            // Wrap other exceptions with context
            throw new TestExecutionException(
                $"Test '{context.TestName}' failed with unexpected exception",
                ex);
        }
    }
}
```

### Hook Executor Exception Handling

```csharp
public class LoggingHookExecutor : IHookExecutor
{
    private readonly ILogger _logger;
    
    public async Task ExecuteAsync(HookContext context, Func<Task> hookBody)
    {
        try
        {
            await hookBody();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Hook {HookType} failed for test {TestName}", 
                context.HookType, 
                context.TestContext.TestName);
                
            // Wrap in appropriate hook exception
            throw context.HookType switch
            {
                HookType.BeforeTest => new BeforeTestException(ex),
                HookType.AfterTest => new AfterTestException(ex),
                HookType.BeforeClass => new BeforeClassException(ex),
                HookType.AfterClass => new AfterClassException(ex),
                _ => ex
            };
        }
    }
}
```

## Best Practices for Exception Handling

### 1. Use Specific Exceptions

Throw the most specific exception type for your scenario:

```csharp
// Good
if (resource == null)
{
    throw new TestFailedInitializationException(
        "Required resource not available");
}

// Less specific
if (resource == null)
{
    throw new Exception("Required resource not available");
}
```

### 2. Preserve Inner Exceptions

Always preserve the original exception as an inner exception:

```csharp
try
{
    await DatabaseSetup();
}
catch (SqlException ex)
{
    throw new BeforeTestException(
        "Database setup failed", 
        ex); // Preserve original exception
}
```

### 3. Add Contextual Information

Include relevant context in exception messages:

```csharp
throw new TestNotFoundException(
    $"Test method '{methodName}' with parameters " +
    $"[{string.Join(", ", parameterTypes)}] not found in class '{className}'");
```

### 4. Handle Async Exceptions Properly

Be careful with async exception handling:

```csharp
[Test]
public async Task AsyncExceptionTest()
{
    try
    {
        await AsyncOperation();
    }
    catch (AggregateException ae)
    {
        // Handle aggregate exceptions from parallel operations
        foreach (var inner in ae.InnerExceptions)
        {
            _logger.LogError(inner, "Parallel operation failed");
        }
        throw;
    }
}
```

### 5. Use Exception Filters When Appropriate

```csharp
try
{
    await TestOperation();
}
catch (TUnitException ex) when (ex.InnerException is SqlException)
{
    // Handle database-related test failures specifically
    await CleanupDatabaseState();
    throw;
}
```

## Creating Custom Exceptions

If you need to create custom exceptions for your test extensions:

```csharp
public class CustomTestException : TUnitException
{
    public string TestCategory { get; }
    public int ErrorCode { get; }
    
    public CustomTestException(
        string message, 
        string testCategory, 
        int errorCode, 
        Exception? innerException = null) 
        : base(message, innerException)
    {
        TestCategory = testCategory;
        ErrorCode = errorCode;
    }
    
    public override string ToString()
    {
        return $"{base.ToString()}\n" +
               $"Category: {TestCategory}\n" +
               $"Error Code: {ErrorCode}";
    }
}
```

## Exception Handling in Test Results

Understanding how exceptions affect test results:

```csharp
[Test]
public async Task ExceptionResultTest()
{
    try
    {
        await RiskyOperation();
    }
    catch (Exception ex)
    {
        // You can access the exception in hooks or event receivers
        // via TestContext.Result.Exception
        
        // The test will be marked as Failed
        // unless it's a SkipTestException (→ Skipped)
        // or InconclusiveTestException (→ Inconclusive)
        throw;
    }
}

[After(HookType.Test)]
public async Task LogTestExceptions()
{
    var result = TestContext.Current?.Result;
    if (result?.Exception != null)
    {
        await LogException(result.Exception);
    }
}
```

## Common Scenarios and Solutions

### Scenario: Flaky External Service

```csharp
[Test]
[Retry(3)]
public async Task ExternalServiceTest()
{
    try
    {
        await CallExternalService();
    }
    catch (HttpRequestException ex) when (IsTransient(ex))
    {
        // Will be retried automatically due to [Retry] attribute
        throw;
    }
    catch (HttpRequestException ex)
    {
        // Non-transient failure - skip instead of fail
        throw new SkipTestException(
            $"External service unavailable: {ex.Message}");
    }
}
```

### Scenario: Resource Cleanup on Failure

```csharp
[Test]
public async Task ResourceTest()
{
    IResource? resource = null;
    try
    {
        resource = await AcquireResource();
        await UseResource(resource);
    }
    catch (Exception ex)
    {
        // Log the exception with context
        _logger.LogError(ex, 
            "Test failed with resource {ResourceId}", 
            resource?.Id);
        throw;
    }
    finally
    {
        // Ensure cleanup happens even on exception
        if (resource != null)
        {
            await ReleaseResource(resource);
        }
    }
}
```

### Scenario: Conditional Test Execution

```csharp
[Before(HookType.Test)]
public async Task CheckEnvironment()
{
    if (!IsCorrectEnvironment())
    {
        throw new SkipTestException(
            "Test requires production-like environment");
    }
    
    if (!await CheckDependencies())
    {
        throw new TestFailedInitializationException(
            "Required dependencies not available");
    }
}
```

## Summary

TUnit's exception hierarchy provides clear, specific exception types for different failure scenarios. By understanding and properly using these exceptions, you can:

- Write more maintainable tests with clear failure reasons
- Implement robust error handling in custom extensions
- Provide better debugging information when tests fail
- Handle different types of failures appropriately

Remember to always preserve context, use specific exception types, and handle async exceptions properly for the best testing experience.