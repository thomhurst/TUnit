# Extension Points

TUnit provides several extension points that allow you to customize and extend the framework's behavior. These interfaces enable you to implement custom test execution logic, hook into the test lifecycle, and control parallel execution.

## ITestExecutor

The `ITestExecutor` interface allows you to customize how tests are executed. This is useful for scenarios like:
- Adding custom logging or telemetry
- Implementing custom retry logic
- Wrapping test execution with special context
- Implementing custom timeout behavior

### Interface Definition

```csharp
public interface ITestExecutor
{
    Task ExecuteAsync(TestContext context, Func<Task> testBody);
}
```

### Example Implementation

```csharp
public class TimingTestExecutor : ITestExecutor
{
    public async Task ExecuteAsync(TestContext context, Func<Task> testBody)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await testBody();
        }
        finally
        {
            stopwatch.Stop();
            context.WriteLine($"Test execution took: {stopwatch.ElapsedMilliseconds}ms");
            
            // You could also send this to telemetry
            TelemetryClient.TrackMetric("TestDuration", stopwatch.ElapsedMilliseconds);
        }
    }
}
```

### Registering a Test Executor

To use your custom test executor, register it in your test assembly:

```csharp
[assembly: RegisterTestExecutor(typeof(TimingTestExecutor))]
```

Or apply it to specific tests or classes:

```csharp
[Test]
[UseTestExecutor(typeof(TimingTestExecutor))]
public async Task MyTest()
{
    // Test logic here
}
```

## IHookExecutor

The `IHookExecutor` interface allows you to customize how setup and cleanup hooks are executed. This is useful for:
- Adding error handling around hooks
- Implementing hook-specific logging
- Managing shared resources during hooks
- Controlling hook execution order

### Interface Definition

```csharp
public interface IHookExecutor
{
    Task ExecuteAsync(HookContext context, Func<Task> hookBody);
}
```

### Example Implementation

```csharp
public class ResourceManagingHookExecutor : IHookExecutor
{
    private static readonly Dictionary<string, IDisposable> Resources = new();
    
    public async Task ExecuteAsync(HookContext context, Func<Task> hookBody)
    {
        if (context.HookType == HookType.Before)
        {
            // Allocate resources before the hook
            var resource = AllocateResource(context.TestContext.TestName);
            Resources[context.TestContext.TestName] = resource;
        }
        
        try
        {
            await hookBody();
        }
        finally
        {
            if (context.HookType == HookType.After)
            {
                // Clean up resources after the hook
                if (Resources.TryGetValue(context.TestContext.TestName, out var resource))
                {
                    resource.Dispose();
                    Resources.Remove(context.TestContext.TestName);
                }
            }
        }
    }
    
    private IDisposable AllocateResource(string testName)
    {
        // Allocate some resource
        return new SomeResource(testName);
    }
}
```

## Event Receivers

TUnit provides several event receiver interfaces that allow you to hook into different stages of the test lifecycle:

### ITestDiscoveryEventReceiver

Notified when tests are discovered during the discovery phase.

```csharp
public interface ITestDiscoveryEventReceiver
{
    Task OnTestDiscoveryStarted(TestDiscoveryContext context);
    Task OnTestDiscovered(DiscoveredTest test);
    Task OnTestDiscoveryCompleted(TestDiscoveryContext context, IReadOnlyList<DiscoveredTest> tests);
}
```

### ITestRegisteredEventReceiver

Notified when tests are registered with the test engine.

```csharp
public interface ITestRegisteredEventReceiver
{
    Task OnTestRegistered(RegisteredTest test);
}
```

### ITestStartEventReceiver

Notified when a test starts execution.

```csharp
public interface ITestStartEventReceiver
{
    Task OnTestStart(TestContext context);
}
```

### ITestEndEventReceiver

Notified when a test completes execution.

```csharp
public interface ITestEndEventReceiver
{
    Task OnTestEnd(TestContext context, TestResult result);
}
```

### ITestRetryEventReceiver

Notified when a test is retried.

```csharp
public interface ITestRetryEventReceiver
{
    Task OnTestRetry(TestContext context, int attemptNumber, Exception lastException);
}
```

### Example Event Receiver Implementation

```csharp
public class TestReporter : ITestStartEventReceiver, ITestEndEventReceiver
{
    private readonly ITestReportingService _reportingService;
    
    public TestReporter(ITestReportingService reportingService)
    {
        _reportingService = reportingService;
    }
    
    public async Task OnTestStart(TestContext context)
    {
        await _reportingService.ReportTestStarted(
            context.TestName,
            context.TestClass.FullName,
            context.TestParameters
        );
    }
    
    public async Task OnTestEnd(TestContext context, TestResult result)
    {
        await _reportingService.ReportTestCompleted(
            context.TestName,
            result.State,
            result.Duration,
            result.Exception?.Message
        );
    }
}
```

### Registering Event Receivers

Register event receivers at the assembly level:

```csharp
[assembly: RegisterEventReceiver(typeof(TestReporter))]
```

## Parallel Execution Control

### IParallelLimit

Controls the maximum degree of parallelism for tests.

```csharp
public interface IParallelLimit
{
    int Limit { get; }
}
```

Example:

```csharp
public class DatabaseParallelLimit : IParallelLimit
{
    public int Limit => 5; // Max 5 database tests in parallel
}

[ParallelLimiter<DatabaseParallelLimit>]
public class DatabaseTests
{
    // All tests in this class will be limited to 5 parallel executions
}
```

### IParallelConstraint

Defines constraints for parallel execution.

```csharp
public interface IParallelConstraint
{
    string ConstraintKey { get; }
}
```

Example:

```csharp
public class SharedResourceConstraint : IParallelConstraint
{
    public string ConstraintKey => "SharedFile";
}

[NotInParallel<SharedResourceConstraint>]
public async Task Test1()
{
    // This test won't run in parallel with other tests
    // that have the same constraint
}

[NotInParallel<SharedResourceConstraint>]
public async Task Test2()
{
    // This test won't run in parallel with Test1
}
```

## IAsyncInitializer

Provides async initialization support for test classes.

```csharp
public interface IAsyncInitializer
{
    Task InitializeAsync();
}
```

Example:

```csharp
public class DatabaseTests : IAsyncInitializer
{
    private DatabaseConnection _connection;
    
    public async Task InitializeAsync()
    {
        _connection = await DatabaseConnection.CreateAsync();
        await _connection.MigrateAsync();
    }
    
    [Test]
    public async Task TestDatabaseOperation()
    {
        // _connection is guaranteed to be initialized
        await _connection.ExecuteAsync("SELECT 1");
    }
}
```

## Best Practices

1. **Keep Extensions Focused**: Each extension should have a single, clear responsibility.

2. **Handle Exceptions Gracefully**: Always wrap the execution of the original body in try-catch blocks.

3. **Avoid State**: Extensions should be stateless when possible. If state is needed, ensure it's thread-safe.

4. **Document Behavior**: Clearly document what your extension does and any side effects.

5. **Test Your Extensions**: Write tests for your custom extensions to ensure they behave correctly.

6. **Consider Performance**: Extensions run for every test, so keep them lightweight.

## Common Use Cases

### Cross-Cutting Concerns
- Logging and telemetry
- Performance monitoring
- Resource management
- Security context setup

### Integration Testing
- Database transaction management
- HTTP client configuration
- Mock server setup/teardown
- Container orchestration

### Compliance and Auditing
- Test execution auditing
- Compliance logging
- Screenshot capture for UI tests
- Result archival

## Example: Database Transaction Extension

Here's a complete example that wraps each test in a database transaction:

```csharp
public class TransactionalTestExecutor : ITestExecutor
{
    public async Task ExecuteAsync(TestContext context, Func<Task> testBody)
    {
        // Get the database connection from DI
        var dbContext = context.GetService<ApplicationDbContext>();
        
        using var transaction = await dbContext.Database.BeginTransactionAsync();
        
        try
        {
            await testBody();
            
            // Rollback instead of commit to keep tests isolated
            await transaction.RollbackAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

[UseTestExecutor(typeof(TransactionalTestExecutor))]
public class DatabaseTests
{
    private readonly ApplicationDbContext _dbContext;
    
    public DatabaseTests(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    [Test]
    public async Task CreateUser_ShouldAddToDatabase()
    {
        // This test runs in a transaction that's rolled back
        var user = new User { Name = "Test User" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        
        var count = await _dbContext.Users.CountAsync();
        await Assert.That(count).IsEqualTo(1);
    }
}
```

This ensures that each test runs in isolation without affecting the database state.