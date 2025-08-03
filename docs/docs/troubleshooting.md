# Troubleshooting Guide

This guide covers common issues you might encounter when using TUnit and their solutions.

## Test Discovery Issues

### Tests Not Being Discovered

**Symptoms:**
- No tests appear in test explorer
- `dotnet test` reports 0 tests
- IDE doesn't show test indicators

**Common Causes and Solutions:**

#### 1. Missing TUnit Package
```xml
<!-- Ensure TUnit is installed -->
<PackageReference Include="TUnit" Version="*" />
```

#### 2. Microsoft.NET.Test.Sdk Conflict
```xml
<!-- Remove this package - it conflicts with TUnit -->
<!-- <PackageReference Include="Microsoft.NET.Test.Sdk" /> -->
```

#### 3. Missing Test Attribute
```csharp
// ❌ Won't be discovered
public void MyTest() { }

// ✅ Will be discovered
[Test]
public void MyTest() { }
```

#### 4. Non-Public Test Methods
```csharp
// ❌ Private methods won't be discovered
[Test]
private void MyTest() { }

// ✅ Public methods will be discovered
[Test]
public void MyTest() { }
```

#### 5. Static Test Methods
```csharp
// ❌ Static methods aren't supported
[Test]
public static void MyTest() { }

// ✅ Instance methods are supported
[Test]
public void MyTest() { }
```

### AOT Compilation Errors

**Symptoms:**
- Build errors mentioning "trim warnings"
- Runtime errors about missing metadata
- "Source generator did not generate" errors

**Solutions:**

#### 1. Enable AOT-Compatible Mode
```xml
<PropertyGroup>
    <IsAotCompatible>true</IsAotCompatible>
    <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
</PropertyGroup>
```

#### 2. Use AOT-Compatible Data Sources
```csharp
// ❌ Reflection-based (may cause AOT issues)
[MethodDataSource(typeof(DataClass), "GetData")]

// ✅ AOT-friendly generic version
[MethodDataSource<DataClass>(nameof(DataClass.GetData))]
```

## Test Execution Issues

### Tests Hanging or Deadlocking

**Symptoms:**
- Tests never complete
- IDE becomes unresponsive during test runs
- Timeout errors

**Common Causes and Solutions:**

#### 1. Async Deadlocks
```csharp
// ❌ Can cause deadlock
[Test]
public void BadAsyncTest()
{
    var result = AsyncMethod().Result; // Blocking on async
}

// ✅ Proper async handling
[Test]
public async Task GoodAsyncTest()
{
    var result = await AsyncMethod();
}
```

#### 2. Parallel Execution Conflicts
```csharp
// If tests access shared resources, prevent parallel execution
[NotInParallel("SharedResource")]
public class DatabaseTests
{
    // Tests in this class won't run in parallel with others
    // that have the same constraint
}
```

#### 3. Circular Dependencies
```csharp
// ❌ Circular dependency causes deadlock
[Test, DependsOn(nameof(Test2))]
public void Test1() { }

[Test, DependsOn(nameof(Test1))]
public void Test2() { }

// ✅ Linear dependencies
[Test]
public void Test1() { }

[Test, DependsOn(nameof(Test1))]
public void Test2() { }
```

### Timeout Exceptions

**Symptoms:**
- `TimeoutException` thrown
- Tests fail after specific duration
- "Test execution timed out" messages

**Solutions:**

#### 1. Increase Timeout
```csharp
[Test]
[Timeout(30000)] // 30 seconds
public async Task LongRunningTest()
{
    await LongOperation();
}
```

#### 2. Global Timeout Configuration
```bash
dotnet test --timeout 60s
```

#### 3. Check for Infinite Loops
```csharp
// Review your test logic for potential infinite loops
[Test]
public async Task PotentiallyInfiniteTest()
{
    while (condition) // Ensure condition can become false
    {
        await Task.Delay(100);
    }
}
```

## Assertion Failures

### Confusing Assertion Messages

**Symptoms:**
- Assertion messages don't clearly indicate the problem
- Expected vs actual values are unclear

**Solutions:**

#### 1. Use Descriptive Assertions
```csharp
// ❌ Generic assertion
await Assert.That(result).IsTrue();

// ✅ Specific assertion with context
await Assert.That(user.IsActive)
    .IsTrue()
    .Because("User should be active after registration");
```

#### 2. Multiple Assertions
```csharp
// Group related assertions for better error reporting
using (Assert.Multiple())
{
    await Assert.That(user.Name).IsEqualTo("John");
    await Assert.That(user.Email).Contains("@");
    await Assert.That(user.Age).IsGreaterThan(0);
}
```

### Floating Point Comparison Issues

**Symptoms:**
- Tests fail due to floating point precision
- Decimal comparisons unexpectedly fail

**Solution:**
```csharp
// ❌ Direct comparison can fail
await Assert.That(0.1 + 0.2).IsEqualTo(0.3);

// ✅ Use tolerance
await Assert.That(0.1 + 0.2).IsEqualTo(0.3).Within(0.0001);
```

## Dependency Injection Issues

### Services Not Available

**Symptoms:**
- `GetRequiredService` throws exceptions
- Null reference exceptions in tests
- "No service registered" errors

**Solutions:**

#### 1. Ensure Services Are Registered
```csharp
// In your test setup or configuration
[Before(HookType.Assembly)]
public static void ConfigureServices()
{
    var services = new ServiceCollection();
    services.AddSingleton<IMyService, MyService>();
    // Register services...
}
```

#### 2. Check Service Lifetime
```csharp
[Test]
public void ServiceLifetimeTest()
{
    // Scoped services need proper scope handling
    using var scope = ServiceProvider.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<IScopedService>();
}
```

## Data-Driven Test Issues

### Data Source Timeout

**Symptoms:**
- "Data source timed out" errors
- Tests fail before execution
- Discovery phase hangs

**Solutions:**

#### 1. Optimize Data Generation
```csharp
// ❌ Slow data generation
public static IEnumerable<User> GetUsers()
{
    return DatabaseQuery.GetAllUsers(); // Expensive operation
}

// ✅ Lightweight data generation
public static IEnumerable<User> GetUsers()
{
    yield return new User { Id = 1, Name = "Test1" };
    yield return new User { Id = 2, Name = "Test2" };
}
```

#### 2. Increase Data Source Timeout
```csharp
// Configure in test assembly attributes or configuration
[assembly: DataSourceTimeout(30000)] // 30 seconds
```

### Matrix Test Explosion

**Symptoms:**
- Thousands of test combinations generated
- Test discovery takes forever
- Out of memory errors

**Solution:**
```csharp
// ❌ Explosive combination
[Test]
[Arguments(1, 2, 3, 4, 5)]
[Arguments("a", "b", "c", "d", "e")]
[Arguments(true, false)]
// Creates 5 × 5 × 2 = 50 combinations!

// ✅ Use specific combinations
[Test]
[Arguments(1, "a", true)]
[Arguments(2, "b", false)]
[Arguments(3, "c", true)]
// Only 3 specific test cases
```

## Memory and Performance Issues

### High Memory Usage

**Symptoms:**
- Out of memory exceptions
- Slow test execution
- System becomes unresponsive

**Solutions:**

#### 1. Dispose Resources Properly
```csharp
[Test]
public async Task ResourceIntensiveTest()
{
    using var largeResource = new LargeResource();
    // Test logic
    // Resource automatically disposed
}
```

#### 2. Limit Parallel Execution
```csharp
[ParallelLimiter<Conservative>]
public class MemoryIntensiveTests
{
    // Limit concurrent execution
}

public class Conservative : IParallelLimit
{
    public int Limit => 2; // Max 2 tests in parallel
}
```

#### 3. Clear Test Data Between Runs
```csharp
[After(HookType.Test)]
public void Cleanup()
{
    GC.Collect(); // Force garbage collection if needed
    _testData.Clear();
}
```

## Hook and Lifecycle Issues

### Hooks Not Executing

**Symptoms:**
- Setup/cleanup code not running
- Database not initialized
- Resources not cleaned up

**Solutions:**

#### 1. Check Hook Scope
```csharp
// ❌ Instance method for class-level hook
[Before(HookType.Class)]
public void ClassSetup() { } // Won't work!

// ✅ Static method for class-level hook
[Before(HookType.Class)]
public static void ClassSetup() { } // Works!
```

#### 2. Verify Hook Order
```csharp
// Hooks execute in this order:
// 1. Assembly Before
// 2. Class Before
// 3. Test Before
// 4. TEST EXECUTION
// 5. Test After
// 6. Class After (after all tests in class)
// 7. Assembly After (after all tests)
```

### Async Initialization Issues

**Symptoms:**
- "Cannot await in constructor" errors
- Resources not ready when test starts

**Solution:**
```csharp
public class DatabaseTests : IAsyncInitializer
{
    private DatabaseConnection _connection;
    
    // Async initialization
    public async Task InitializeAsync()
    {
        _connection = await DatabaseConnection.CreateAsync();
    }
    
    [Test]
    public async Task TestDatabase()
    {
        // _connection is guaranteed to be initialized
    }
}
```

## IDE Integration Issues

### Visual Studio Test Explorer Issues

**Symptoms:**
- Tests not showing in Test Explorer
- "Run Test" option missing
- Test status not updating

**Solutions:**

1. **Clean and Rebuild**
   ```bash
   dotnet clean
   dotnet build
   ```

2. **Clear Test Cache**
   - Close Visual Studio
   - Delete `.vs` folder
   - Reopen and rebuild

3. **Update Test Platform**
   ```xml
   <PackageReference Include="Microsoft.TestPlatform" Version="*" />
   ```

### VS Code Test Explorer Issues

**Solutions:**

1. **Install C# Dev Kit**
   - Ensure latest version is installed

2. **Configure Test Settings**
   ```json
   {
     "dotnetCoreExplorer.testProjectPath": "**/*.csproj"
   }
   ```

## Platform-Specific Issues

### Linux/macOS File Path Issues

**Symptoms:**
- Tests fail on Linux/macOS but pass on Windows
- "File not found" errors

**Solution:**
```csharp
// ❌ Windows-specific paths
var path = @"C:\TestData\file.txt";

// ✅ Cross-platform paths
var path = Path.Combine("TestData", "file.txt");
```

### Line Ending Issues

**Solution:**
```csharp
// ❌ Hard-coded line endings
var expected = "Line1\r\nLine2";

// ✅ Platform-agnostic
var expected = $"Line1{Environment.NewLine}Line2";
```

## Debugging Tips

### Enable Diagnostic Logging

```bash
# Run with diagnostic output
dotnet test --logger "console;verbosity=detailed"

# Enable TUnit diagnostics
dotnet test -- --diagnostic
```

### Attach Debugger to Test

```csharp
[Test]
public void DebuggableTest()
{
    #if DEBUG
    Debugger.Launch(); // Prompts to attach debugger
    #endif
    
    // Test logic
}
```

### Capture Test Output

```csharp
[Test]
public async Task TestWithOutput()
{
    TestContext.Current?.WriteLine("Debug: Starting test");
    
    var result = await Operation();
    
    TestContext.Current?.WriteLine($"Debug: Result = {result}");
    
    await Assert.That(result).IsNotNull();
}
```

## Getting Help

If you're still experiencing issues:

1. **Check the Documentation**: Review relevant sections of the TUnit documentation
2. **Search Issues**: Check [GitHub Issues](https://github.com/thomhurst/TUnit/issues) for similar problems
3. **Enable Diagnostics**: Run with `--diagnostic` flag for detailed logs
4. **Create Minimal Reproduction**: Isolate the issue in a small test project
5. **Report Issue**: If it's a bug, report it with:
   - TUnit version
   - .NET version
   - Minimal code to reproduce
   - Full error messages and stack traces

Remember to check for updates - many issues are resolved in newer versions of TUnit.