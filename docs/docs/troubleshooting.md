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

### Assertion Not Awaited (Test Passes Without Checking)

**Symptoms:**
- Test passes but assertion never executes
- Compiler warning: "This async method lacks 'await' operators"
- Test passes when it should fail

**Root Cause:**

Forgetting to `await` an assertion means it returns a `Task` that's never executed. The test completes immediately without checking anything.

**Example:**

```csharp
[Test]
public async Task BadTest()
{
    var result = Calculate(2, 2);

    // Wrong - missing await
    Assert.That(result).IsEqualTo(5);  // Returns Task, never awaited

    // Test passes because assertion never runs
}
```

**Solution:**

Always await assertions:

```csharp
[Test]
public async Task GoodTest()
{
    var result = Calculate(2, 2);
    await Assert.That(result).IsEqualTo(4);
}
```

**Prevention:**

The compiler warns you about this (CS4014: "Because this call is not awaited..."). To catch these at build time, enable treating warnings as errors:

```xml
<PropertyGroup>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

See also: [FAQ: Why do I have to await all assertions?](faq.md#why-do-i-have-to-await-all-assertions-can-i-use-synchronous-assertions)

### Array and Collection Comparison Issues

**Symptoms:**
- "IsEqualTo doesn't work for arrays"
- Arrays with same values fail equality check
- Error messages about reference equality vs value equality

**Root Cause:**

Arrays use reference equality by default. You need to use collection-specific assertion methods.

#### Comparing Arrays

```csharp
var expected = new[] { 1, 2, 3 };
var actual = new[] { 1, 2, 3 };

// Wrong - compares references, not values
await Assert.That(actual).IsEqualTo(expected);  // Fails

// Correct - use IsEquivalentTo for collections
await Assert.That(actual).IsEquivalentTo(expected);  // Passes
```

Note that `IsEquivalentTo` ignores order. If order matters, assert on elements individually:

```csharp
await Assert.That(actual).HasCount().EqualTo(expected.Length);
for (int i = 0; i < expected.Length; i++)
{
    await Assert.That(actual[i]).IsEqualTo(expected[i]);
}
```

#### Arrays of Complex Types

```csharp
var expected = new[]
{
    new User { Id = 1, Name = "Alice" },
    new User { Id = 2, Name = "Bob" }
};

// May not work without custom equality implementation
await Assert.That(actual).IsEquivalentTo(expected);

// More reliable - assert on properties
await Assert.That(actual).HasCount().EqualTo(2);
await Assert.That(actual[0].Name).IsEqualTo("Alice");
await Assert.That(actual[1].Name).IsEqualTo("Bob");

// Or compare projected values
await Assert.That(actual.Select(u => u.Name))
    .IsEquivalentTo(new[] { "Alice", "Bob" });
```

#### Arrays of Tuples (Known Limitation)

```csharp
var expected = new[] { (1, "a"), (2, "b") };
var actual = new[] { (1, "a"), (2, "b") };

// Current limitation - may not work as expected
// await Assert.That(actual).IsEquivalentTo(expected);

// Workaround - assert individual elements
await Assert.That(actual).HasCount().EqualTo(2);
await Assert.That(actual[0]).IsEqualTo((1, "a"));
await Assert.That(actual[1]).IsEqualTo((2, "b"));
```

#### Lists and Other Collections

```csharp
var list = new List<int> { 1, 2, 3 };

// Works for IEnumerable types
await Assert.That(list).IsEquivalentTo(new[] { 1, 2, 3 });

// Check specific properties
await Assert.That(list).HasCount().EqualTo(3);
await Assert.That(list).Contains(2);
await Assert.That(list).DoesNotContain(5);
```

**General Approach:**
- Use `IsEquivalentTo` for unordered collection comparison
- Iterate and assert elements for ordered comparison
- Assert on key properties for complex types
- Consider implementing `IEquatable<T>` on your types for cleaner assertions

### Assertion on Wrong Type

**Symptoms:**
- Compiler error: "Cannot convert from 'X' to 'Y'"
- Assertion method not available for type
- IntelliSense doesn't show expected assertions

#### String vs Object Assertions

```csharp
object value = "hello";

// Doesn't compile - object doesn't have string-specific assertions
// await Assert.That(value).StartsWith("h");

// Cast to the correct type
await Assert.That((string)value).StartsWith("h");

// Or check the type first
await Assert.That(value).IsTypeOf<string>();
await Assert.That((string)value).StartsWith("h");
```

#### Nullable Values

```csharp
int? nullableInt = 5;

// Option 1: Check for null, then access value
await Assert.That(nullableInt).IsNotNull();
await Assert.That(nullableInt!.Value).IsEqualTo(5);

// Option 2: Use HasValue pattern
await Assert.That(nullableInt.HasValue).IsTrue();
await Assert.That(nullableInt.GetValueOrDefault()).IsEqualTo(5);
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

## Code Coverage Issues

### Coverage Files Not Generated

**Symptoms:**
- No coverage files in TestResults folder
- `--coverage` flag has no effect
- Coverage reports empty or missing

**Common Causes and Solutions:**

#### 1. Using TUnit.Engine Without Extensions
```xml
<!-- ❌ Missing coverage extension -->
<PackageReference Include="TUnit.Engine" Version="*" />

<!-- ✅ Includes coverage automatically -->
<PackageReference Include="TUnit" Version="*" />
```

**Fix:** Use the TUnit meta package, or manually add the coverage extension if using TUnit.Engine directly:
```xml
<PackageReference Include="TUnit.Engine" Version="*" />
<PackageReference Include="Microsoft.Testing.Extensions.CodeCoverage" Version="*" />
```

#### 2. Using .NET 7 or Earlier
```bash
# Check your .NET version
dotnet --version
```

**Requirements:**
- Ensure you have a recent .NET SDK installed
- Microsoft.Testing.Platform supports .NET Standard 2.0+

**Tip:** Use a recent .NET SDK version for the best experience:
```xml
<TargetFramework>net8.0</TargetFramework>
```

#### 3. Configuration Not Set to Release
```bash
# It's generally better to run coverage in Release configuration
dotnet test --configuration Release --coverage
```

### Coverlet Still Installed

**Symptoms:**
- Coverage stopped working after migrating to TUnit
- Conflicts between coverage tools
- "Could not load file or assembly" errors related to coverage

**Root Cause:** Coverlet is **not compatible** with TUnit because:
- Coverlet requires VSTest platform
- TUnit uses Microsoft.Testing.Platform
- These platforms are mutually exclusive

**Solution:**

1. **Remove Coverlet packages** from your `.csproj`:
```xml
<!-- Remove these lines -->
<PackageReference Include="coverlet.collector" Version="*" />
<PackageReference Include="coverlet.msbuild" Version="*" />
```

2. **Ensure TUnit meta package is installed**:
```xml
<PackageReference Include="TUnit" Version="*" />
```

3. **Update coverage commands**:
```bash
# Old (VSTest + Coverlet)
dotnet test --collect:"XPlat Code Coverage"

# New (TUnit + Microsoft Coverage)
dotnet run --configuration Release --coverage
```

See the [Code Coverage FAQ](faq.md#does-tunit-work-with-coverlet-for-code-coverage) for more details.

### Missing Coverage for Some Assemblies

**Symptoms:**
- Coverage reports show 0% for some projects
- Some assemblies excluded from coverage
- Unexpected gaps in coverage

**Solutions:**

#### 1. Create a `.runsettings` File
```xml
<!-- coverage.runsettings -->
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="Code Coverage">
        <Configuration>
          <CodeCoverage>
            <ModulePaths>
              <Include>
                <ModulePath>.*\.dll$</ModulePath>
                <ModulePath>.*MyProject\.dll$</ModulePath>
              </Include>
              <Exclude>
                <ModulePath>.*tests?\.dll$</ModulePath>
                <ModulePath>.*TestHelpers\.dll$</ModulePath>
              </Exclude>
            </ModulePaths>
          </CodeCoverage>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

#### 2. Use the Settings File
```bash
dotnet run --configuration Release --coverage --coverage-settings coverage.runsettings
```

### Coverage Format Not Recognized by CI/CD

**Symptoms:**
- CI/CD doesn't display coverage results
- Coverage upload fails
- "Unsupported format" errors

**Solutions:**

#### 1. Check Output Format
```bash
# Default is Cobertura (widely supported)
dotnet run --configuration Release --coverage --coverage-output-format cobertura

# For Visual Studio
dotnet run --configuration Release --coverage --coverage-output-format xml

# Multiple formats
dotnet run --configuration Release --coverage \
  --coverage-output-format cobertura \
  --coverage-output-format xml
```

#### 2. Verify Output Location
```bash
# Coverage files generated in TestResults by default
ls TestResults/

# Expected files:
# - coverage.cobertura.xml
# - <guid>/coverage.xml
```

#### 3. Common CI/CD Configurations

**GitHub Actions:**
```yaml
- name: Run tests with coverage
  run: dotnet run --project tests/MyProject.Tests --configuration Release --coverage

- name: Upload coverage to Codecov
  uses: codecov/codecov-action@v3
  with:
    files: ./tests/MyProject.Tests/TestResults/coverage.cobertura.xml
```

**Azure Pipelines:**
```yaml
- task: DotNetCoreCLI@2
  inputs:
    command: 'run'
    projects: 'tests/**/*.csproj'
    arguments: '--configuration Release --coverage --coverage-output $(Agent.TempDirectory)/coverage/'

- task: PublishCodeCoverageResults@2
  inputs:
    summaryFileLocation: '$(Agent.TempDirectory)/coverage/**/coverage.cobertura.xml'
```

### Coverage Percentage Seems Wrong

**Symptoms:**
- Coverage percentage doesn't match expectations
- Test code included in coverage
- Dependencies inflating coverage numbers

**Solutions:**

#### 1. Exclude Test Projects
```xml
<!-- coverage.runsettings -->
<ModulePaths>
  <Exclude>
    <ModulePath>.*tests?\.dll$</ModulePath>
    <ModulePath>.*\.Tests\.dll$</ModulePath>
  </Exclude>
</ModulePaths>
```

#### 2. Exclude Generated Code
```xml
<ModulePaths>
  <Exclude>
    <ModulePath>.*\.g\.cs$</ModulePath>
    <ModulePath>.*\.Designer\.cs$</ModulePath>
  </Exclude>
</ModulePaths>
```

#### 3. Include Only Production Code
```xml
<ModulePaths>
  <Include>
    <ModulePath>.*MyCompany\.MyProduct\..*\.dll$</ModulePath>
  </Include>
  <Exclude>
    <ModulePath>.*tests?\.dll$</ModulePath>
  </Exclude>
</ModulePaths>
```

### Need More Help with Coverage?

See also:
- [Code Coverage FAQ](faq.md#does-tunit-work-with-coverlet-for-code-coverage)
- [Code Coverage Documentation](extensions/extensions.md#code-coverage)
- [xUnit Migration - Code Coverage](migration/xunit.md#code-coverage)
- [NUnit Migration - Code Coverage](migration/nunit.md#code-coverage)
- [MSTest Migration - Code Coverage](migration/mstest.md#code-coverage)
- [Microsoft's Coverage Documentation](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage)

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