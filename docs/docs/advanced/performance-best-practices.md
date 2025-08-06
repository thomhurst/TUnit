# Performance Best Practices

This guide provides recommendations for optimizing test performance and ensuring your TUnit test suite runs efficiently.

## Test Discovery Performance

### Use AOT Mode

TUnit's AOT (Ahead-of-Time) compilation mode provides the best performance for test discovery:

```xml
<PropertyGroup>
    <IsAotCompatible>true</IsAotCompatible>
    <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
</PropertyGroup>
```

Benefits:
- Faster test discovery
- Lower memory usage
- Better performance in CI/CD pipelines

### Optimize Data Sources

#### Keep Data Generation Lightweight

```csharp
// ❌ Bad: Heavy computation during discovery
public static IEnumerable<User> GetTestUsers()
{
    // This runs during test discovery!
    var users = DatabaseQuery.GetAllUsers();
    return users.Where(u => u.IsActive);
}

// ✅ Good: Lightweight data generation
public static IEnumerable<User> GetTestUsers()
{
    yield return new User { Id = 1, Name = "Test User 1" };
    yield return new User { Id = 2, Name = "Test User 2" };
}
```

#### Use Lazy Data Loading

```csharp
// ✅ Good: Defer expensive operations until test execution
[Test]
[MethodDataSource<LazyDataProvider>(nameof(LazyDataProvider.GetIds))]
public async Task TestWithLazyData(int id)
{
    // Load full data only during test execution
    var user = await LoadUserAsync(id);
    await Assert.That(user).IsNotNull();
}

public class LazyDataProvider
{
    public static IEnumerable<int> GetIds()
    {
        // Return only IDs during discovery
        yield return 1;
        yield return 2;
        yield return 3;
    }
}
```

### Limit Matrix Test Combinations

```csharp
// ❌ Bad: Exponential test explosion
[Test]
[Arguments(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)]
[Arguments("a", "b", "c", "d", "e")]
[Arguments(true, false)]
// Creates 10 × 5 × 2 = 100 tests!

// ✅ Good: Targeted test combinations
[Test]
[Arguments(1, "a", true)]
[Arguments(5, "c", false)]
[Arguments(10, "e", true)]
// Only 3 specific test cases
```

## Test Execution Performance

### Optimize Parallel Execution

#### Configure Appropriate Parallelism

```csharp
// Set maximum parallel test execution
[assembly: MaxParallelTests(Environment.ProcessorCount)]

// Or use command line
// dotnet test --maximum-parallel-tests 8
```

#### Group Related Tests

```csharp
// Tests in the same group run sequentially but different groups run in parallel
[ParallelGroup("DatabaseTests")]
public class UserRepositoryTests
{
    // These tests share database resources
}

[ParallelGroup("DatabaseTests")]
public class OrderRepositoryTests
{
    // These also share database resources
}

[ParallelGroup("ApiTests")]
public class ApiIntegrationTests
{
    // These can run in parallel with database tests
}
```

#### Use Parallel Limiters Wisely

```csharp
public class DatabaseConnectionLimit : IParallelLimit
{
    public int Limit => 5; // Max 5 concurrent database connections
}

[ParallelLimiter<DatabaseConnectionLimit>]
public class DatabaseIntegrationTests
{
    // All tests here respect the connection limit
}
```

### Minimize Test Setup Overhead

#### Share Expensive Setup

```csharp
// ❌ Bad: Expensive setup per test
public class ExpensiveTests
{
    [Before(HookType.Test)]
    public async Task SetupEachTest()
    {
        await StartDatabaseContainer();
        await MigrateDatabase();
    }
}

// ✅ Good: Share setup across tests
public class EfficientTests
{
    private static DatabaseContainer? _container;
    
    [Before(HookType.Class)]
    public static async Task SetupOnce()
    {
        _container = await StartDatabaseContainer();
        await MigrateDatabase();
    }
    
    [After(HookType.Class)]
    public static async Task CleanupOnce()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}
```

#### Use Lazy Initialization

```csharp
public class PerformantTests
{
    private static readonly Lazy<ExpensiveResource> _resource = 
        new(() => new ExpensiveResource(), LazyThreadSafetyMode.ExecutionAndPublication);
    
    [Test]
    public async Task TestUsingResource()
    {
        var resource = _resource.Value; // Only created on first access
        await resource.DoSomethingAsync();
    }
}
```

### Optimize Assertions

#### Avoid Expensive Operations in Assertions

```csharp
// ❌ Bad: Expensive operation in assertion
await Assert.That(await GetAllUsersFromDatabase())
    .HasCount()
    .EqualTo(1000);

// ✅ Good: Use efficient queries
var userCount = await GetUserCountFromDatabase();
await Assert.That(userCount).IsEqualTo(1000);
```

#### Use Early Exit Patterns

```csharp
[Test]
public async Task EfficientValidation()
{
    var result = await GetResultAsync();
    
    // Quick checks first
    if (result == null)
    {
        await Assert.That(result).IsNotNull();
        return; // Exit early
    }
    
    // More expensive validations only if needed
    await Assert.That(result.Items).HasCount().GreaterThan(0);
}
```

## Memory Management

### Dispose Resources Properly

```csharp
public class MemoryEfficientTests : IAsyncDisposable
{
    private readonly List<IDisposable> _disposables = new();
    
    [Test]
    public async Task TestWithResources()
    {
        var resource = new LargeResource();
        _disposables.Add(resource);
        
        // Use resource
        await resource.ProcessAsync();
    }
    
    public async ValueTask DisposeAsync()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
        
        // Force garbage collection if needed
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
```

### Avoid Memory Leaks in Static Fields

```csharp
// ❌ Bad: Static collections that grow indefinitely
public class LeakyTests
{
    private static readonly List<TestResult> _allResults = new();
    
    [After(HookType.Test)]
    public void StoreResult()
    {
        _allResults.Add(GetCurrentResult()); // Memory leak!
    }
}

// ✅ Good: Proper cleanup or bounded collections
public class EfficientTests
{
    private static readonly Queue<TestResult> _recentResults = new();
    private const int MaxResults = 100;
    
    [After(HookType.Test)]
    public void StoreResult()
    {
        _recentResults.Enqueue(GetCurrentResult());
        
        while (_recentResults.Count > MaxResults)
        {
            _recentResults.Dequeue();
        }
    }
}
```

### Use ValueTask for High-Frequency Operations

```csharp
// For operations called many times, use ValueTask to reduce allocations
public async ValueTask<bool> FastCheckAsync(int id)
{
    if (_cache.TryGetValue(id, out var cached))
    {
        return cached; // No allocation for cached path
    }
    
    var result = await LoadFromDatabaseAsync(id);
    _cache[id] = result;
    return result;
}

[Test]
[Arguments(1, 2, 3, 4, 5)] // Many invocations
public async Task HighFrequencyTest(int id)
{
    var result = await FastCheckAsync(id);
    await Assert.That(result).IsTrue();
}
```

## I/O Performance

### Batch Operations

```csharp
// ❌ Bad: Individual operations
[Test]
public async Task SlowIOTest()
{
    foreach (var id in Enumerable.Range(1, 100))
    {
        await SaveUserAsync(new User { Id = id });
    }
}

// ✅ Good: Batch operations
[Test]
public async Task FastIOTest()
{
    var users = Enumerable.Range(1, 100)
        .Select(id => new User { Id = id })
        .ToList();
    
    await SaveUsersBatchAsync(users);
}
```

### Use Async I/O

```csharp
// ❌ Bad: Synchronous I/O
[Test]
public void SyncIOTest()
{
    var content = File.ReadAllText("large-file.txt");
    ProcessContent(content);
}

// ✅ Good: Asynchronous I/O
[Test]
public async Task AsyncIOTest()
{
    var content = await File.ReadAllTextAsync("large-file.txt");
    await ProcessContentAsync(content);
}
```

### Cache File Contents

```csharp
public class FileTestsWithCache
{
    private static readonly ConcurrentDictionary<string, string> _fileCache = new();
    
    private async Task<string> GetFileContentAsync(string path)
    {
        return await _fileCache.GetOrAddAsync(path, 
            async p => await File.ReadAllTextAsync(p));
    }
    
    [Test]
    [Arguments("config1.json")]
    [Arguments("config2.json")]
    public async Task TestWithCachedFiles(string filename)
    {
        var content = await GetFileContentAsync(filename);
        await Assert.That(content).IsNotEmpty();
    }
}
```

## Database Testing Performance

### Use Transaction Rollback

```csharp
public class FastDatabaseTests
{
    [Test]
    public async Task TransactionalTest()
    {
        using var transaction = await BeginTransactionAsync();
        
        try
        {
            // Perform database operations
            await CreateUserAsync("test@example.com");
            
            // Verify
            var user = await GetUserAsync("test@example.com");
            await Assert.That(user).IsNotNull();
            
            // Rollback instead of cleanup
            await transaction.RollbackAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

### Use In-Memory Databases for Unit Tests

```csharp
public class InMemoryDatabaseTests
{
    private DbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
            
        return new AppDbContext(options);
    }
    
    [Test]
    public async Task FastDatabaseTest()
    {
        using var context = CreateInMemoryContext();
        
        // Test runs entirely in memory
        context.Users.Add(new User { Name = "Test" });
        await context.SaveChangesAsync();
        
        var count = await context.Users.CountAsync();
        await Assert.That(count).IsEqualTo(1);
    }
}
```

## CI/CD Optimization

### Split Test Suites

```bash
# Run fast unit tests first
dotnet test --filter "Category=Unit" --no-build

# Run slower integration tests separately
dotnet test --filter "Category=Integration" --no-build

# Run expensive E2E tests last
dotnet test --filter "Category=E2E" --no-build
```

### Use Test Result Caching

```xml
<!-- Cache test results in CI -->
<PropertyGroup>
    <TUnitCacheTestResults>true</TUnitCacheTestResults>
    <TUnitTestResultsCachePath>$(Build.StagingDirectory)/testcache</TUnitTestResultsCachePath>
</PropertyGroup>
```

### Fail Fast in CI

```bash
# Stop on first failure to save CI time
dotnet test --fail-fast
```

## Monitoring and Profiling

### Add Performance Logging

```csharp
public class PerformanceAwareExecutor : ITestExecutor
{
    private readonly ILogger<PerformanceAwareExecutor> _logger;
    
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
            
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning(
                    "Slow test detected: {TestName} took {ElapsedMs}ms",
                    context.TestDetails.TestName,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
```

### Track Test Metrics

```csharp
[After(HookType.Test)]
public static void RecordTestMetrics()
{
    var context = TestContext.Current;
    if (context?.Result != null)
    {
        TelemetryClient.TrackMetric(
            "TestDuration",
            context.Result.Duration.TotalMilliseconds,
            new Dictionary<string, string>
            {
                ["TestName"] = context.TestDetails.TestName,
                ["TestClass"] = context.TestDetails.TestClass,
                ["Result"] = context.Result.State.ToString()
            });
    }
}
```

## Summary

Key performance principles:

1. **Optimize Discovery**: Keep data sources lightweight and limit test combinations
2. **Parallelize Wisely**: Use appropriate parallel limits and grouping
3. **Manage Resources**: Dispose properly and avoid memory leaks
4. **Cache Aggressively**: Cache expensive operations and file I/O
5. **Batch Operations**: Group database and I/O operations
6. **Monitor Performance**: Track and alert on slow tests
7. **Use AOT Mode**: Enable AOT for best performance
8. **Fail Fast**: Stop early on failures in CI

By following these practices, you can maintain a fast, efficient test suite that scales with your codebase.