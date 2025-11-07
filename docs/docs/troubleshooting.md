# Troubleshooting & FAQ

This guide covers common questions and issues you might encounter when using TUnit.

## Frequently Asked Questions

These are conceptual questions about TUnit's design and capabilities.

### Why do I have to await all assertions? Can I use synchronous assertions?

All TUnit assertions must be awaited. There's no synchronous alternative.

**Important:** Test methods themselves can be either synchronous (`void`) or asynchronous (`async Task`). However, if your test uses TUnit's assertion library (`Assert.That(...)`), the test method **must** be `async Task` because assertions return awaitable objects that must be awaited to execute. Tests without assertions can remain synchronous. See [Test Method Signatures](getting-started/writing-your-first-test.md#test-method-signatures) for examples.

**Why this design?**

TUnit's assertion library uses the awaitable pattern (custom objects with `GetAwaiter()` methods). This means:
- Assertions don't execute until they're awaited - this is when the actual verification happens
- All assertions work consistently, whether they're simple value checks or complex async operations
- Custom assertions can perform async work (like database queries or HTTP calls)
- No sync-over-async patterns that cause deadlocks
- Assertions can be chained fluently before execution

**What this means when migrating:**

You need to convert your tests to `async Task` and add `await` before assertions.

Before (xUnit/NUnit/MSTest):
```csharp
[Test]
public void MyTest()
{
    var result = Calculate(2, 3);
    Assert.Equal(5, result);
}
```

After (TUnit):
```csharp
[Test]
public async Task MyTest()
{
    var result = Calculate(2, 3);
    await Assert.That(result).IsEqualTo(5);
}
```

**Automated migration**

TUnit includes code fixers that handle most of this conversion for you:

```bash
# For xUnit
dotnet format analyzers --severity info --diagnostics TUXU0001

# For NUnit
dotnet format analyzers --severity info --diagnostics TUNU0001

# For MSTest
dotnet format analyzers --severity info --diagnostics TUMS0001
```

The code fixer converts test methods to async, adds await to assertions, and updates attribute names. It handles most common cases automatically, though you may need to adjust complex scenarios manually.

See the migration guides for step-by-step instructions:
- [xUnit migration](migration/xunit.md#automated-migration-with-code-fixers)
- [NUnit migration](migration/nunit.md#automated-migration-with-code-fixers)
- [MSTest migration](migration/mstest.md#automated-migration-with-code-fixers)

**What you gain**

Async assertions enable patterns that aren't possible with synchronous assertions:

```csharp
[Test]
public async Task AsyncAssertion_Example()
{
    // Await async operations in assertions
    await Assert.That(async () => await GetUserAsync(123))
        .Throws<UserNotFoundException>();

    // Chain assertions naturally
    var user = await GetUserAsync(456);
    await Assert.That(user.Email)
        .IsNotNull()
        .And.Contains("@example.com");
}
```

**Watch out for missing awaits**

The most common mistake is forgetting `await`. The compiler warns you, but the test will pass without actually running the assertion:

```csharp
// Wrong - test passes without checking anything
Assert.That(result).IsEqualTo(5);  // Returns an awaitable object that's never executed

// Correct
await Assert.That(result).IsEqualTo(5);  // The await triggers the actual assertion execution
```

### Does TUnit work with Coverlet for code coverage?

**No.** Coverlet (`coverlet.collector` or `coverlet.msbuild`) is **not compatible** with TUnit.

**Why?** TUnit uses the modern `Microsoft.Testing.Platform` instead of the legacy VSTest platform. Coverlet only works with VSTest.

**Solution:** Use `Microsoft.Testing.Extensions.CodeCoverage` instead, which is:
- ✅ **Automatically included** with the TUnit meta package
- ✅ Provides the same functionality as Coverlet
- ✅ Outputs Cobertura and XML formats
- ✅ Works with all major CI/CD systems

See the [Code Coverage section](#code-coverage-issues) below for usage instructions.

### What code coverage tool should I use with TUnit?

Use **Microsoft.Testing.Extensions.CodeCoverage**, which is:
- ✅ **Already included** with the TUnit package (no manual installation)
- ✅ Built and maintained by Microsoft
- ✅ Works seamlessly with Microsoft.Testing.Platform
- ✅ Outputs industry-standard formats (Cobertura, XML)
- ✅ Compatible with all major CI/CD systems and coverage viewers

**Do not use:**
- ❌ Coverlet (incompatible with Microsoft.Testing.Platform)

---

## Common Problems & Solutions

This section provides symptom-based troubleshooting for specific issues.

## Test Discovery Issues

### Tests Not Being Discovered

**Symptoms:**
- No tests appear in test explorer
- `dotnet test` reports 0 tests
- IDE doesn't show test indicators

**Common Error Messages:**
- `[Microsoft.Testing.Platform] No test found`
- `0 Tests Passed, 0 Tests Failed, 0 Tests Skipped`

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

**Error Message:** `Program has more than one entry point defined`

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

#### 6. Wrong OutputType in Project File

**Error Message:** `A fatal error occurred. The required library hostfxr.dll could not be found.`

```xml
<!-- ❌ Wrong output type -->
<OutputType>Library</OutputType>

<!-- ✅ Correct output type -->
<OutputType>Exe</OutputType>
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

**Common Error Messages:**
- `System.TimeoutException: The operation has timed out`
- `Test execution exceeded timeout of 30000ms`

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

**Common Error Messages:**
- `CS4014: Because this call is not awaited, execution of the current method continues before the call is completed`

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

Note that `IsEquivalentTo` ignores order by default. If order matters, use `CollectionOrdering.Matching`:

```csharp
// Order doesn't matter (default)
await Assert.That(actual).IsEquivalentTo(expected);  // Passes even if order differs

// Order must match exactly
await Assert.That(actual).IsEquivalentTo(expected, CollectionOrdering.Matching);
```

Or assert on elements individually:

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
- Use `IsEquivalentTo` for unordered collection comparison (default)
- Use `IsEquivalentTo(expected, CollectionOrdering.Matching)` for ordered comparison
- Iterate and assert elements individually for complex ordered comparisons
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

## Common Migration Pitfalls (from xUnit/NUnit/MSTest)

If you're migrating from another testing framework, these are the most common issues you'll encounter.

### Understanding the Platform Change

**The Core Shift:** TUnit uses `Microsoft.Testing.Platform` instead of the legacy `VSTest` platform. This fundamental change affects several aspects of your testing workflow.

**What This Means:**

1. **Different Test Runners**
   - VSTest used `vstest.console.exe` and `Microsoft.NET.Test.Sdk`
   - TUnit uses the modern `Microsoft.Testing.Platform`
   - They are **mutually exclusive** - you cannot use both

2. **Different Commands**
   ```bash
   # Old (VSTest)
   dotnet test --collect:"XPlat Code Coverage"

   # New (TUnit)
   dotnet run --configuration Release --coverage
   ```

3. **Different Package Requirements**
   ```xml
   <!-- Old (VSTest) -->
   <PackageReference Include="Microsoft.NET.Test.Sdk" Version="*" />
   <PackageReference Include="coverlet.collector" Version="*" />

   <!-- New (TUnit) -->
   <PackageReference Include="TUnit" Version="*" />
   <!-- Coverage is included automatically -->
   ```

### Tests Don't Appear in IDE Test Explorer

**Symptoms:**
- Tests worked in xUnit/NUnit but don't show in Visual Studio/Rider
- Test Explorer is empty
- "Run Test" gutter icons don't appear

**Root Cause:** IDE needs to be configured for Microsoft.Testing.Platform support.

**Solutions:**

**Visual Studio:**
1. Go to Tools > Options > Preview Features
2. Enable "Use testing platform server mode"
3. Restart Visual Studio
4. Rebuild your solution

**Rider:**
1. Go to Settings > Build, Execution, Deployment > Unit Testing > Testing Platform
2. Enable "Testing Platform support"
3. Restart Rider
4. Rebuild your solution

**VS Code:**
1. Install C# Dev Kit extension
2. Go to extension settings
3. Enable "Dotnet > Test Window > Use Testing Platform Protocol"
4. Reload window

### Command Line Differences

**Old Way (VSTest):**
```bash
dotnet test
dotnet test --filter "Category=Integration"
dotnet test --logger "trx;LogFileName=results.trx"
```

**New Way (TUnit):**
```bash
dotnet run
dotnet run -- --treenode-filter "/*/*/*/*[Category=Integration]"
dotnet run -- --report-trx --report-trx-filename results.trx
```

**Key Differences:**
- Use `dotnet run` instead of `dotnet test` for best experience
- Arguments after `--` are passed to the test application
- Filter syntax is different (tree-node based)
- Reporting flags have different names

### .runsettings File Migration

**Old (.runsettings for VSTest):**
```xml
<RunConfiguration>
  <MaxCpuCount>4</MaxCpuCount>
  <ResultsDirectory>./TestResults</ResultsDirectory>
</RunConfiguration>
```

**New (TUnit configuration):**

TUnit uses command-line flags or programmatic configuration instead of `.runsettings`:

```bash
# Parallel execution
dotnet run -- --parallel

# Custom results directory
dotnet run -- --results-directory ./TestResults
```

For more complex configuration, use the programmatic API in your test setup.

## Testing with External Dependencies

Real-world tests often interact with databases, APIs, and file systems. Here's how to handle these effectively.

### Database Testing

**Strategy 1: In-Memory Providers**

Best for unit tests that need a database but don't test database-specific behavior.

```csharp
public class UserRepositoryTests
{
    private DbContext _context;

    [Before(Test)]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
    }

    [Test]
    public async Task CanSaveAndRetrieveUser()
    {
        // Arrange
        var user = new User { Name = "Alice", Email = "alice@example.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var retrieved = await _context.Users.FirstOrDefaultAsync(u => u.Email == "alice@example.com");

        // Assert
        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.Name).IsEqualTo("Alice");
    }

    [After(Test)]
    public void Cleanup()
    {
        _context.Dispose();
    }
}
```

**Strategy 2: Test Containers (Testcontainers)**

Best for integration tests that need real database behavior.

```csharp
public class DatabaseIntegrationTests : IAsyncInitializer, IAsyncDisposable
{
    private PostgreSqlContainer _container;
    private DbContext _context;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithDatabase("testdb")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.MigrateAsync();
    }

    [Test]
    public async Task DatabaseTransactionTest()
    {
        // Test with real database
        var user = new User { Name = "Bob" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await Assert.That(user.Id).IsGreaterThan(0);
    }

    public async ValueTask DisposeAsync()
    {
        _context?.Dispose();
        if (_container != null)
            await _container.DisposeAsync();
    }
}
```

**Strategy 3: Shared Database Fixture**

For multiple tests sharing the same database setup:

```csharp
[NotInParallel("SharedDatabase")]
public class SharedDatabaseTests
{
    private static DbContext _sharedContext;

    [Before(HookType.Class)]
    public static async Task ClassSetup()
    {
        _sharedContext = await SetupDatabaseAsync();
    }

    [Before(HookType.Test)]
    public async Task TestSetup()
    {
        // Clear data between tests
        _sharedContext.Users.RemoveRange(_sharedContext.Users);
        await _sharedContext.SaveChangesAsync();
    }

    [Test]
    public async Task Test1()
    {
        // Use _sharedContext
    }

    [After(HookType.Class)]
    public static async Task ClassCleanup()
    {
        _sharedContext?.Dispose();
    }
}
```

### Mocking HTTP Calls and External APIs

**Strategy 1: Using Moq with HttpClient**

```csharp
public class WeatherServiceTests
{
    [Test]
    public async Task GetWeather_ReturnsTemperature()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"temperature\": 22.5}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new WeatherService(httpClient);

        // Act
        var weather = await service.GetWeatherAsync("London");

        // Assert
        await Assert.That(weather.Temperature).IsEqualTo(22.5);
    }
}
```

**Strategy 2: WireMock for Integration Tests**

```csharp
public class ApiIntegrationTests : IAsyncInitializer, IAsyncDisposable
{
    private WireMockServer _mockServer;
    private HttpClient _httpClient;

    public async Task InitializeAsync()
    {
        _mockServer = WireMockServer.Start();
        _httpClient = new HttpClient { BaseAddress = new Uri(_mockServer.Urls[0]) };

        await Task.CompletedTask;
    }

    [Test]
    public async Task ApiCall_HandlesSuccessResponse()
    {
        // Setup mock response
        _mockServer
            .Given(Request.Create().WithPath("/api/users").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("[{\"id\": 1, \"name\": \"Alice\"}]"));

        // Act
        var response = await _httpClient.GetStringAsync("/api/users");

        // Assert
        await Assert.That(response).Contains("Alice");
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();
        _mockServer?.Stop();
        await Task.CompletedTask;
    }
}
```

### File System Testing

**Best Practices:**

1. **Use Temporary Directories**
2. **Clean Up After Tests**
3. **Use Path.Combine for Cross-Platform Compatibility**

```csharp
public class FileProcessorTests
{
    private string _testDirectory;

    [Before(Test)]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [Test]
    public async Task ProcessFile_CreatesOutputFile()
    {
        // Arrange
        var inputFile = Path.Combine(_testDirectory, "input.txt");
        var outputFile = Path.Combine(_testDirectory, "output.txt");

        await File.WriteAllTextAsync(inputFile, "test content");

        var processor = new FileProcessor();

        // Act
        await processor.ProcessFileAsync(inputFile, outputFile);

        // Assert
        await Assert.That(File.Exists(outputFile)).IsTrue();
        var content = await File.ReadAllTextAsync(outputFile);
        await Assert.That(content).Contains("processed");
    }

    [After(Test)]
    public void Cleanup()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}
```

**Using IFileSystem Abstraction (Recommended):**

```csharp
// Production code uses IFileSystem interface
public class DocumentService
{
    private readonly IFileSystem _fileSystem;

    public DocumentService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task SaveDocumentAsync(string path, string content)
    {
        await _fileSystem.File.WriteAllTextAsync(path, content);
    }
}

// Test with mock file system
public class DocumentServiceTests
{
    [Test]
    public async Task SaveDocument_WritesToFile()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var service = new DocumentService(mockFileSystem);

        // Act
        await service.SaveDocumentAsync("/docs/test.txt", "content");

        // Assert
        await Assert.That(mockFileSystem.File.Exists("/docs/test.txt")).IsTrue();
        var content = await mockFileSystem.File.ReadAllTextAsync("/docs/test.txt");
        await Assert.That(content).IsEqualTo("content");
    }
}
```

## Configuration and Secrets Management

One of the most common challenges in testing is loading configuration from `appsettings.json`, environment variables, or user secrets.

### Configuration File Not Found

**Symptoms:**
- `FileNotFoundException: Could not find file 'appsettings.json'`
- Configuration values are null or default
- "The configuration file 'appsettings.json' was not found"

**Common Error Messages:**
- `System.IO.FileNotFoundException: Could not find file 'C:\...\bin\Debug\net8.0\appsettings.json'`

**Root Cause:**

Test projects run from the `bin/Debug/net8.0` directory, but your `appsettings.json` file is in the project root. The file isn't being copied to the output directory.

**Solution:**

#### 1. Configure CopyToOutputDirectory in .csproj

```xml
<ItemGroup>
  <None Update="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
  <None Update="appsettings.Development.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

**Options:**
- `PreserveNewest` - Copy only if the file is newer (recommended)
- `Always` - Always copy the file (slower builds)

### Loading IConfiguration in Tests

**Recommended Pattern:**

```csharp
public class ConfigurationTests
{
    private static IConfiguration _configuration;

    [Before(HookType.Class)]
    public static void SetupConfiguration()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    [Test]
    public async Task CanLoadConfiguration()
    {
        var connectionString = _configuration.GetConnectionString("Default");
        await Assert.That(connectionString).IsNotNull();
    }
}
```

### Environment-Specific Configuration

**Problem:** Need different settings for Development, CI, Production testing.

**Solution:**

#### 1. Use Environment-Specific Files

```csharp
[Before(HookType.Class)]
public static void SetupConfiguration()
{
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

    _configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{environment}.json", optional: true)
        .AddEnvironmentVariables()
        .Build();
}
```

**In .csproj:**
```xml
<ItemGroup>
  <None Update="appsettings*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

#### 2. Set Environment in CI/CD

**GitHub Actions:**
```yaml
- name: Run tests
  env:
    ASPNETCORE_ENVIRONMENT: CI
  run: dotnet run --project tests/MyProject.Tests
```

**Azure Pipelines:**
```yaml
- task: DotNetCoreCLI@2
  env:
    ASPNETCORE_ENVIRONMENT: CI
  inputs:
    command: 'run'
    projects: 'tests/**/*.csproj'
```

### User Secrets in Tests

**Problem:** Need to test with secrets (API keys, passwords) without committing them.

**Solution:**

#### 1. Initialize User Secrets (One Time)

```bash
cd MyProject.Tests
dotnet user-secrets init
```

This adds a `UserSecretsId` to your `.csproj`:
```xml
<PropertyGroup>
  <UserSecretsId>your-unique-guid</UserSecretsId>
</PropertyGroup>
```

#### 2. Add Secrets

```bash
dotnet user-secrets set "ApiKey" "my-secret-key"
dotnet user-secrets set "ConnectionStrings:Database" "Server=localhost;..."
```

#### 3. Load Secrets in Tests

```csharp
[Before(HookType.Class)]
public static void SetupConfiguration()
{
    _configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false)
        .AddUserSecrets<ConfigurationTests>() // Load user secrets
        .AddEnvironmentVariables()
        .Build();
}
```

**Install Package:**
```xml
<PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="*" />
```

### Configuration from Environment Variables

**Recommended for CI/CD:**

```csharp
[Before(HookType.Class)]
public static void SetupConfiguration()
{
    _configuration = new ConfigurationBuilder()
        .AddEnvironmentVariables("MYAPP_") // Prefix optional
        .Build();
}

[Test]
public async Task UsesEnvironmentVariable()
{
    var apiKey = _configuration["ApiKey"]; // Reads MYAPP_ApiKey
    await Assert.That(apiKey).IsNotNull();
}
```

**Setting Environment Variables:**

**Windows (PowerShell):**
```powershell
$env:MYAPP_ApiKey = "secret-key"
dotnet run --project tests/MyProject.Tests
```

**Linux/macOS:**
```bash
export MYAPP_ApiKey="secret-key"
dotnet run --project tests/MyProject.Tests
```

**Inline:**
```bash
MYAPP_ApiKey="secret-key" dotnet run --project tests/MyProject.Tests
```

### Strongly-Typed Configuration

**Recommended Pattern:**

```csharp
// Configuration class
public class AppSettings
{
    public string ApiUrl { get; set; }
    public int Timeout { get; set; }
    public ConnectionStrings ConnectionStrings { get; set; }
}

public class ConnectionStrings
{
    public string Default { get; set; }
}

// In tests
public class ConfigurationTests
{
    private static AppSettings _settings;

    [Before(HookType.Class)]
    public static void SetupConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        _settings = config.Get<AppSettings>();
    }

    [Test]
    public async Task CanAccessTypedConfiguration()
    {
        await Assert.That(_settings.ApiUrl).IsNotNull();
        await Assert.That(_settings.Timeout).IsGreaterThan(0);
    }
}
```

### Common Configuration Patterns

#### Pattern 1: Shared Configuration for All Tests

```csharp
public class TestBase
{
    protected static IConfiguration Configuration { get; private set; }

    [Before(HookType.Assembly)]
    public static void SetupSharedConfiguration()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
    }
}

// In your tests
public class MyTests : TestBase
{
    [Test]
    public async Task UsesSharedConfiguration()
    {
        var value = Configuration["Setting"];
        await Assert.That(value).IsNotNull();
    }
}
```

#### Pattern 2: Per-Test Configuration

```csharp
public class PerTestConfigTests
{
    private IConfiguration _configuration;

    [Before(Test)]
    public void SetupTestConfiguration()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["TestSetting"] = "test-value"
            })
            .Build();
    }

    [Test]
    public async Task UsesTestSpecificConfiguration()
    {
        await Assert.That(_configuration["TestSetting"]).IsEqualTo("test-value");
    }
}
```

### Troubleshooting Configuration Issues

#### Issue: Configuration is null after loading

**Check:**
1. Is the file being copied? Check `bin/Debug/net8.0/` for `appsettings.json`
2. Is the file path correct? Use `SetBasePath(Directory.GetCurrentDirectory())`
3. Is the file marked as `Content` or `None` with `CopyToOutputDirectory`?

**Debug:**
```csharp
[Before(HookType.Class)]
public static void SetupConfiguration()
{
    var currentDir = Directory.GetCurrentDirectory();
    TestContext.Current?.WriteLine($"Current directory: {currentDir}");

    var files = Directory.GetFiles(currentDir, "*.json");
    TestContext.Current?.WriteLine($"JSON files: {string.Join(", ", files)}");

    _configuration = new ConfigurationBuilder()
        .SetBasePath(currentDir)
        .AddJsonFile("appsettings.json", optional: false)
        .Build();
}
```

#### Issue: Configuration values are wrong

**Check binding:**
```csharp
[Test]
public void DebugConfiguration()
{
    var allConfig = _configuration.AsEnumerable();
    foreach (var kvp in allConfig)
    {
        TestContext.Current?.WriteLine($"{kvp.Key} = {kvp.Value}");
    }
}
```

#### Issue: Secrets not loading in CI/CD

**Solution:** User secrets only work locally. In CI/CD, use environment variables:

```yaml
# GitHub Actions
- name: Run tests
  env:
    ApiKey: ${{ secrets.API_KEY }}
  run: dotnet run --project tests/MyProject.Tests
```

## Test Filtering and Grouping Issues

When you have hundreds or thousands of tests, filtering becomes critical for running the right subset.

### Filter Not Selecting Expected Tests

**Symptoms:**
- `--filter` command selects no tests or wrong tests
- All tests run when you expected a subset
- Category filters don't work

**Common Error Messages:**
- `No tests matched the specified filter`
- `0 tests discovered`

**Root Cause:**

TUnit uses a **tree-node filter syntax**, not the legacy VSTest filter syntax.

### Tree-Node Filter Syntax

**Pattern:** `/Assembly/Namespace/Class/Method[Property=Value]`

**Examples:**

```bash
# Run all tests in a specific class
dotnet run -- --treenode-filter "/*/*/MyTestClass/*"

# Run a specific test method
dotnet run -- --treenode-filter "/*/*/MyTestClass/MyTestMethod"

# Run tests with a specific category
dotnet run -- --treenode-filter "/*/*/*/*[Category=Integration]"

# Run tests NOT in a category
dotnet run -- --treenode-filter "/*/*/*/*[Category!=Performance]"

# Multiple filters (OR)
dotnet run -- --treenode-filter "/*/*/ClassA/*|/*/*/ClassB/*"

# Combine filters (AND)
dotnet run -- --treenode-filter "/*/*/*/*[Category=Integration][Priority=High]"
```

### Common Filter Patterns

#### Run All Tests in Namespace

```bash
dotnet run -- --treenode-filter "/*/MyNamespace.*/*"
```

#### Run Tests by Category

```csharp
[Test]
[Category("Integration")]
public async Task DatabaseTest() { }

[Test]
[Category("Unit")]
public async Task CalculationTest() { }
```

```bash
# Run integration tests
dotnet run -- --treenode-filter "/*/*/*/*[Category=Integration]"

# Run unit tests
dotnet run -- --treenode-filter "/*/*/*/*[Category=Unit]"

# Run everything except performance tests
dotnet run -- --treenode-filter "/*/*/*/*[Category!=Performance]"
```

#### Run Tests by Property

```csharp
[Test]
[Property("Owner", "TeamA")]
public async Task FeatureTest() { }
```

```bash
dotnet run -- --treenode-filter "/*/*/*/*[Owner=TeamA]"
```

### Categories Not Being Applied

**Symptoms:**
- Category filter returns no tests
- Tests don't appear with expected category

**Common Causes:**

#### 1. Category Attribute on Class vs Method

```csharp
// ✅ Category on test method
[Test]
[Category("Integration")]
public async Task MyTest() { }

// ✅ Category on class (applies to all tests in class)
[Category("Integration")]
public class IntegrationTests
{
    [Test]
    public async Task Test1() { }

    [Test]
    public async Task Test2() { }
}
```

#### 2. Typo in Category Name

```csharp
[Test]
[Category("Intergration")] // ❌ Typo!
public async Task MyTest() { }
```

```bash
# Won't find the test
dotnet run -- --treenode-filter "/*/*/*/*[Category=Integration]"
```

**Solution:** Use constants to avoid typos:

```csharp
public static class TestCategories
{
    public const string Integration = nameof(Integration);
    public const string Unit = nameof(Unit);
    public const string Performance = nameof(Performance);
}

[Test]
[Category(TestCategories.Integration)]
public async Task MyTest() { }
```

### Combining Filters in CI/CD

**Problem:** Need different test suites for different CI stages.

**Solution:**

#### GitHub Actions Example

```yaml
jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - name: Run unit tests
        run: dotnet run --project tests/MyProject.Tests -- --treenode-filter "/*/*/*/*[Category=Unit]"

  integration-tests:
    runs-on: ubuntu-latest
    needs: unit-tests
    steps:
      - name: Run integration tests
        run: dotnet run --project tests/MyProject.Tests -- --treenode-filter "/*/*/*/*[Category=Integration]"

  smoke-tests:
    runs-on: ubuntu-latest
    steps:
      - name: Run smoke tests only
        run: dotnet run --project tests/MyProject.Tests -- --treenode-filter "/*/*/*/*[Category=Smoke]"
```

#### Azure Pipelines Example

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Unit Tests'
  inputs:
    command: 'run'
    projects: 'tests/**/*.csproj'
    arguments: '-- --treenode-filter "/*/*/*/*[Category=Unit]"'

- task: DotNetCoreCLI@2
  displayName: 'Integration Tests'
  inputs:
    command: 'run'
    projects: 'tests/**/*.csproj'
    arguments: '-- --treenode-filter "/*/*/*/*[Category=Integration]"'
```

### Excluding Tests from Runs

**Problem:** Some tests should never run in CI (e.g., manual tests, local-only tests).

**Solution:**

#### 1. Use Explicit Attribute

```csharp
[Test]
[Explicit] // Won't run unless explicitly requested
public async Task ManualTest() { }
```

#### 2. Use Custom Category

```csharp
[Test]
[Category("ManualOnly")]
public async Task InteractiveTest() { }
```

```bash
# CI runs everything except manual tests
dotnet run -- --treenode-filter "/*/*/*/*[Category!=ManualOnly]"
```

### Multiple Categories on Same Test

```csharp
[Test]
[Category("Integration")]
[Category("Database")]
[Category("Slow")]
public async Task ComplexTest() { }
```

```bash
# Run tests that are BOTH Integration AND Database
dotnet run -- --treenode-filter "/*/*/*/*[Category=Integration][Category=Database]"

# Run tests that are Integration OR Unit
dotnet run -- --treenode-filter "/*/*/*/*[Category=Integration]|/*/*/*/*[Category=Unit]"
```

### Debugging Filter Issues

**Enable Diagnostic Output:**

```bash
dotnet run -- --treenode-filter "your-filter" --diagnostic
```

This shows which tests are discovered and why they were included/excluded.

**List All Tests Without Running:**

```bash
# Use --list-tests flag if available, or run with dry-run
dotnet run -- --help
```

**Verify Test Discovery:**

```bash
# Run without filter to see all tests
dotnet run

# Count tests discovered
dotnet run | grep "Test Passed"
```

### Best Practices for Test Organization

**1. Use Hierarchical Categories**

```csharp
public static class TestCategories
{
    public const string Unit = "Unit";
    public const string Integration = "Integration";

    public static class Integration
    {
        public const string Database = "Integration.Database";
        public const string Api = "Integration.Api";
        public const string FileSystem = "Integration.FileSystem";
    }
}
```

**2. Consistent Naming**

```csharp
// ✅ Good: Clear, consistent
[Category("Integration")]
[Category("Database")]

// ❌ Bad: Inconsistent
[Category("integration")] // lowercase
[Category("DB")] // abbreviation
```

**3. Document Your Categories**

Create a `TestCategories.md` in your test project:

```markdown
# Test Categories

- `Unit` - Fast, isolated unit tests
- `Integration` - Tests with external dependencies
- `Integration.Database` - Database integration tests
- `Integration.Api` - API integration tests
- `Performance` - Performance/load tests (excluded from CI)
- `Smoke` - Critical path smoke tests (run first in CI)
```

## Diagnosing Flaky Tests

Flaky tests pass or fail inconsistently. They're one of the most frustrating issues in test suites.

### Common Causes

#### 1. Race Conditions in Parallel Tests

**Symptom:** Test passes when run alone but fails when run with other tests.

```csharp
// ❌ Flaky - tests modify shared state
public class CounterTests
{
    private static int _counter = 0;

    [Test]
    public async Task IncrementCounter()
    {
        _counter++;
        await Assert.That(_counter).IsEqualTo(1); // Fails in parallel
    }
}

// ✅ Fixed - use NotInParallel or instance state
[NotInParallel("Counter")]
public class CounterTests
{
    private int _counter = 0; // Instance variable, not static

    [Test]
    public async Task IncrementCounter()
    {
        _counter++;
        await Assert.That(_counter).IsEqualTo(1); // Always passes
    }
}
```

#### 2. Un-Awaited Async Operations

**Symptom:** Test sometimes passes, sometimes times out or fails.

```csharp
// ❌ Flaky - not waiting for background work
[Test]
public async Task ProcessData()
{
    var processor = new DataProcessor();
    processor.StartBackgroundWork(); // Fire-and-forget

    await Assert.That(processor.IsComplete).IsTrue(); // Race condition!
}

// ✅ Fixed - properly await async work
[Test]
public async Task ProcessData()
{
    var processor = new DataProcessor();
    await processor.ProcessAsync(); // Wait for completion

    await Assert.That(processor.IsComplete).IsTrue();
}
```

#### 3. System Time Dependencies

**Symptom:** Test fails at different times of day or in different time zones.

```csharp
// ❌ Flaky - depends on current time
[Test]
public async Task IsBusinessHours()
{
    var service = new BusinessHoursService();
    var result = service.IsBusinessHours(); // Uses DateTime.Now

    await Assert.That(result).IsTrue(); // Fails at night!
}

// ✅ Fixed - inject time provider
[Test]
public async Task IsBusinessHours()
{
    var mockTime = new Mock<ITimeProvider>();
    mockTime.Setup(t => t.Now).Returns(new DateTime(2024, 1, 15, 10, 0, 0)); // Monday 10 AM

    var service = new BusinessHoursService(mockTime.Object);
    var result = service.IsBusinessHours();

    await Assert.That(result).IsTrue(); // Always passes
}
```

#### 4. External Service Dependencies

**Symptom:** Test fails when network is slow or service is down.

```csharp
// ❌ Flaky - depends on external API
[Test]
public async Task FetchUserData()
{
    var client = new HttpClient();
    var response = await client.GetStringAsync("https://api.example.com/users/1");

    await Assert.That(response).Contains("username"); // Fails if API is down
}

// ✅ Fixed - mock the HTTP call
[Test]
public async Task FetchUserData()
{
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
        .Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage
        {
            Content = new StringContent("{\"username\": \"alice\"}")
        });

    var client = new HttpClient(mockHandler.Object);
    var response = await client.GetStringAsync("https://api.example.com/users/1");

    await Assert.That(response).Contains("username"); // Always passes
}
```

### Strategies for Reproducing Flaky Tests

1. **Run Tests Multiple Times**
   ```bash
   # Run test 100 times to expose flakiness
   for i in {1..100}; do dotnet run -- --treenode-filter "/*/*/*/FlakyTest"; done
   ```

2. **Run with Maximum Parallelism**
   ```bash
   dotnet run -- --parallel --max-parallel-threads 8
   ```

3. **Add Delays to Expose Race Conditions**
   ```csharp
   [Test]
   public async Task TestWithDelay()
   {
       await Task.Delay(Random.Shared.Next(0, 100)); // Random delay
       // Test logic
   }
   ```

4. **Enable Detailed Logging**
   ```csharp
   [Test]
   public async Task TestWithLogging()
   {
       TestContext.Current?.WriteLine($"Starting test at {DateTime.Now:O}");
       // Test logic
       TestContext.Current?.WriteLine($"Completed test at {DateTime.Now:O}");
   }
   ```

## Dependency Injection Issues

### Services Not Available

**Symptoms:**
- `GetRequiredService` throws exceptions
- Null reference exceptions in tests
- "No service registered" errors

**Common Error Messages:**
- `InvalidOperationException: No service for type 'IMyService' has been registered`

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

**Common Error Messages:**
- `OutOfMemoryException: Insufficient memory to continue the execution`

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

1. **Enable Testing Platform Support**
   - Tools > Options > Preview Features
   - Enable "Use testing platform server mode"
   - Restart Visual Studio

2. **Clean and Rebuild**
   ```bash
   dotnet clean
   dotnet build
   ```

3. **Clear Test Cache**
   - Close Visual Studio
   - Delete `.vs` folder
   - Reopen and rebuild

### Rider Test Explorer Issues

**Solutions:**

1. **Enable Testing Platform Support**
   - Settings > Build, Execution, Deployment > Unit Testing > Testing Platform
   - Enable "Testing Platform support"
   - Restart Rider

2. **Invalidate Caches**
   - File > Invalidate Caches / Restart
   - Choose "Invalidate and Restart"

### VS Code Test Explorer Issues

**Solutions:**

1. **Install C# Dev Kit**
   - Ensure latest version is installed
   - Install from Extensions marketplace

2. **Configure Test Settings**
   ```json
   {
     "dotnet.testWindow.useTestingPlatformProtocol": true
   }
   ```

3. **Reload Window**
   - Ctrl+Shift+P > "Developer: Reload Window"

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

**Common Error Messages:**
- `No coverage data collected`
- `Coverage tool initialization failed`

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

#### 2. Configuration Not Set to Release
```bash
# It's generally better to run coverage in Release configuration
dotnet run --configuration Release --coverage
```

#### 3. Basic Coverage Commands
```bash
# Basic usage
dotnet run --configuration Release --coverage

# With output location
dotnet run --configuration Release --coverage --coverage-output ./coverage/

# Specify format (cobertura, xml, etc.)
dotnet run --configuration Release --coverage --coverage-output-format cobertura
```

### Coverlet Still Installed

**Symptoms:**
- Coverage stopped working after migrating to TUnit
- Conflicts between coverage tools
- "Could not load file or assembly" errors related to coverage

**Common Error Messages:**
- `System.IO.FileNotFoundException: Could not load file or assembly 'Coverlet.Core'`

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
    files: ./tests/MyProject.Tests/TestResults/**/coverage.cobertura.xml
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

## Debugging Tips

### Enable Diagnostic Logging

```bash
# Run with diagnostic output
dotnet test --logger "console;verbosity=detailed"

# Enable TUnit diagnostics
dotnet run -- --diagnostic
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
