# Migrating from xUnit.net

:::from-xunit Performance Boost
Migrating from xUnit to TUnit can improve test execution speed. Check the [benchmarks](/docs/benchmarks) to see how TUnit compares.
:::

## Quick Reference

| xUnit | TUnit |
|-------|-------|
| `[Fact]` | `[Test]` |
| `[Theory]` | `[Test]` |
| `[InlineData(...)]` | `[Arguments(...)]` |
| `[MemberData(nameof(...))]` | `[MethodDataSource(nameof(...))]` |
| `[ClassData(typeof(...))]` | `[MethodDataSource(nameof(ClassName.Method))]` |
| `[Trait("key", "value")]` | `[Property("key", "value")]` |
| `IClassFixture<T>` | `[ClassDataSource<T>(Shared = SharedType.PerClass)]` |
| `[Collection("name")]` | `[ClassDataSource<T>(Shared = SharedType.Keyed, Key = "name")]` |
| Constructor | Constructor or `[Before(Test)]` |
| `IDisposable` | `IDisposable` or `[After(Test)]` |
| `IAsyncLifetime` | `[Before(Test)]` / `[After(Test)]` |
| `ITestOutputHelper` | `TestContext` parameter |
| `Assert.Equal(expected, actual)` | `await Assert.That(actual).IsEqualTo(expected)` |
| `Assert.Throws<T>(() => ...)` | `await Assert.ThrowsAsync<T>(() => ...)` |

## Automated Migration with Code Fixers

TUnit includes Roslyn analyzers and code fixers that automate most of the migration work. The `TUXU0001` diagnostic identifies xUnit code patterns and provides automatic fixes to convert them to TUnit equivalents.

**What gets converted automatically:**
- `[Fact]` → `[Test]`
- `[Theory]` → `[Test]`
- `[InlineData(...)]` → `[Arguments(...)]`
- `[MemberData(nameof(...))]` → `[MethodDataSource(nameof(...))]`
- `[Trait("key", "value")]` → `[Property("key", "value")]`
- `Assert.Equal(expected, actual)` → `await Assert.That(actual).IsEqualTo(expected)`
- `Assert.True(condition)` → `await Assert.That(condition).IsTrue()`
- `Assert.Throws<T>(...)` → `await Assert.ThrowsAsync<T>(...)`
- `Assert.Contains(item, collection)` → `await Assert.That(collection).Contains(item)`
- Test methods converted to `async Task` with `await` on assertions

The code fixer handles roughly 80-90% of typical test suites automatically.

**What requires manual adjustment:**
- `IClassFixture<T>` → `[ClassDataSource<T>(Shared = SharedType.PerClass)]` on the class
- `ICollectionFixture<T>` and `[Collection("name")]` → `[ClassDataSource<T>(Shared = SharedType.Keyed, Key = "name")]`
- `IAsyncLifetime` → `[Before(Test)]` and `[After(Test)]` methods
- `ITestOutputHelper` → `TestContext` parameter injection
- Custom `MemberData` return types (convert `IEnumerable<object[]>` to `IEnumerable<(...)>` tuples)
- `[ClassData(typeof(...))]` → `[MethodDataSource(nameof(ClassName.Method))]`
- Constructor injection of fixtures → primary constructor with `[ClassDataSource<T>]` attribute
- Collection definitions → remove `ICollectionFixture` classes entirely

If you find a common pattern that should be automated but isn't, please [open an issue](https://github.com/thomhurst/TUnit/issues).

### Prerequisites

- .NET SDK 8.0 or later (for `dotnet format` with analyzer support)
- TUnit packages installed in your test project

### Step-by-Step Migration

:::tip Safety First
Commit your changes or create a backup before running the code fixer. This allows you to review changes and revert if needed.
:::

**1. Install TUnit packages**

Add the TUnit packages to your test project alongside xUnit (temporarily):

```bash
dotnet add package TUnit
```

**2. Disable TUnit's implicit usings (temporary)**

Add these properties to your `.csproj` to prevent type name conflicts between xUnit and TUnit:

```xml
<PropertyGroup>
    <TUnitImplicitUsings>false</TUnitImplicitUsings>
    <TUnitAssertionsImplicitUsings>false</TUnitAssertionsImplicitUsings>
</PropertyGroup>
```

This allows the code fixer to distinguish between `Xunit.Assert` and `TUnit.Assertions.Assert`.

**3. Rebuild the project**

```bash
dotnet build
```

This restores packages and loads the TUnit analyzers. You should see `TUXU0001` warnings in your build output for xUnit code that can be converted.

**4. Run the automated code fixer**

```bash
dotnet format analyzers --severity info --diagnostics TUXU0001
```

This command applies all available fixes for the `TUXU0001` diagnostic. You'll see output indicating which files were modified.

:::warning Multi-targeting Projects
If your project targets multiple .NET versions (e.g., `net8.0;net9.0;net10.0`), you **must** specify a single target framework when running the code fixer. Multi-targeting can cause the code fixer to crash with the error `Changes must be within bounds of SourceText` due to a limitation in Roslyn's linked file handling.

**Option 1:** Specify a single framework via command line:
```bash
dotnet format analyzers --severity info --diagnostics TUXU0001 --framework net10.0
```

**Option 2:** Temporarily modify your project file to single-target:
```xml
<!-- Before migration -->
<TargetFramework>net10.0</TargetFramework>
<!-- <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks> -->
```

Run the code fixer, then restore multi-targeting afterward. Replace `net10.0` with your project's highest supported target framework.
:::

**5. Remove the implicit usings workaround**

Remove or comment out the properties you added in step 2:

```xml
<!-- Remove these lines -->
<PropertyGroup>
    <TUnitImplicitUsings>false</TUnitImplicitUsings>
    <TUnitAssertionsImplicitUsings>false</TUnitAssertionsImplicitUsings>
</PropertyGroup>
```

**6. Fix remaining issues manually**

Build the project and address any remaining compilation errors:

```bash
dotnet build
```

Common manual fixes needed:
- Replace `IClassFixture<T>` with `[ClassDataSource<T>(Shared = SharedType.PerClass)]` attribute
- Replace `IAsyncLifetime` with `[Before(Test)]`/`[After(Test)]` methods
- Replace `ITestOutputHelper` constructor parameter with `TestContext` method parameter
- Convert data source methods to return tuples instead of `object[]`
- Add `using TUnit.Core;` and `using TUnit.Assertions;` if not using implicit usings
- Remove `ICollectionFixture` and collection definition classes

**7. Remove xUnit packages**

Once everything compiles and tests pass:

```bash
dotnet remove package xunit
dotnet remove package xunit.runner.visualstudio
```

**8. Verify the migration**

```bash
dotnet build
dotnet run -- --list-tests
```

### Troubleshooting

**Code fixer doesn't run / no files changed:**
- Ensure you rebuilt after adding TUnit packages
- Check that `TUXU0001` warnings appear in build output
- Try running with verbose output: `dotnet format analyzers --severity info --diagnostics TUXU0001 --verbosity detailed`

**Build errors after running code fixer:**
- Missing `await` keywords: ensure test methods are `async Task`
- Ambiguous `Assert`: remove xUnit usings or fully qualify types
- Type mismatch in data sources: convert `IEnumerable<object[]>` returns to `IEnumerable<(...)>` tuples

**IClassFixture not converted:**
- This requires manual conversion - add `[ClassDataSource<T>(Shared = SharedType.PerClass)]` to the class
- Use a primary constructor to receive the fixture: `public class MyTests(MyFixture fixture)`

**Analyzers not loading:**
- Verify TUnit package is installed: `dotnet list package`
- Try cleaning and rebuilding: `dotnet clean && dotnet build`

## Manual Migration Guide

### Basic Test Structure

#### Simple Test (Fact → Test)

**xUnit Code:**
```csharp
public class CalculatorTests
{
    [Fact]
    public void Add_TwoNumbers_ReturnsSum()
    {
        var calculator = new Calculator();
        var result = calculator.Add(2, 3);
        Assert.Equal(5, result);
    }
}
```

**TUnit Equivalent:**
```csharp
public class CalculatorTests
{
    [Test]
    public async Task Add_TwoNumbers_ReturnsSum()
    {
        var calculator = new Calculator();
        var result = calculator.Add(2, 3);
        await Assert.That(result).IsEqualTo(5);
    }
}
```

**Key Changes:**
- `[Fact]` → `[Test]`
- Test method returns `async Task`
- Assertions use fluent syntax with `await Assert.That(...)`

### Parameterized Tests

#### Theory with InlineData → Arguments

**xUnit Code:**
```csharp
public class StringTests
{
    [Theory]
    [InlineData("hello", 5)]
    [InlineData("world", 5)]
    [InlineData("", 0)]
    public void Length_ReturnsCorrectValue(string input, int expectedLength)
    {
        Assert.Equal(expectedLength, input.Length);
    }
}
```

**TUnit Equivalent:**
```csharp
public class StringTests
{
    [Test]
    [Arguments("hello", 5)]
    [Arguments("world", 5)]
    [Arguments("", 0)]
    public async Task Length_ReturnsCorrectValue(string input, int expectedLength)
    {
        await Assert.That(input.Length).IsEqualTo(expectedLength);
    }
}
```

**Key Changes:**
- `[Theory]` → `[Test]`
- `[InlineData(...)]` → `[Arguments(...)]`
- Method is async and assertions are awaited

### Data Sources

#### MemberData → MethodDataSource

**xUnit Code:**
```csharp
public class DataDrivenTests
{
    [Theory]
    [MemberData(nameof(GetTestData))]
    public void ProcessData_WithVariousInputs(int value, string text, bool expected)
    {
        var result = SomeLogic(value, text);
        Assert.Equal(expected, result);
    }

    public static IEnumerable<object[]> GetTestData()
    {
        yield return new object[] { 1, "test", true };
        yield return new object[] { 2, "demo", false };
        yield return new object[] { 3, "example", true };
    }
}
```

**TUnit Equivalent:**
```csharp
public class DataDrivenTests
{
    [Test]
    [MethodDataSource(nameof(GetTestData))]
    public async Task ProcessData_WithVariousInputs(int value, string text, bool expected)
    {
        var result = SomeLogic(value, text);
        await Assert.That(result).IsEqualTo(expected);
    }

    public static IEnumerable<(int value, string text, bool expected)> GetTestData()
    {
        yield return (1, "test", true);
        yield return (2, "demo", false);
        yield return (3, "example", true);
    }
}
```

**Key Changes:**
- `[MemberData(nameof(...))]` → `[MethodDataSource(nameof(...))]`
- Data source returns tuples instead of `object[]` (strongly typed)
- No need for boxing/unboxing values

#### ClassData → MethodDataSource

**xUnit Code:**
```csharp
public class TestDataGenerator : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { 1, "one" };
        yield return new object[] { 2, "two" };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class MyTests
{
    [Theory]
    [ClassData(typeof(TestDataGenerator))]
    public void TestWithClassData(int number, string text)
    {
        Assert.NotNull(text);
    }
}
```

**TUnit Equivalent:**
```csharp
public class MyTests
{
    [Test]
    [MethodDataSource(nameof(TestDataGenerator.GetTestData))]
    public async Task TestWithClassData(int number, string text)
    {
        await Assert.That(text).IsNotNull();
    }
}

public class TestDataGenerator
{
    public static IEnumerable<(int, string)> GetTestData()
    {
        yield return (1, "one");
        yield return (2, "two");
    }
}
```

**Key Changes:**
- `[ClassData(typeof(...))]` → `[MethodDataSource(nameof(ClassName.MethodName))]`
- Point to a static method rather than implementing IEnumerable
- Use tuples for type safety

### Setup and Teardown

#### Constructor and IDisposable → Before/After Hooks

**xUnit Code:**
```csharp
public class DatabaseTests : IDisposable
{
    private readonly DatabaseConnection _connection;

    public DatabaseTests()
    {
        _connection = new DatabaseConnection();
        _connection.Open();
    }

    [Fact]
    public void Query_ReturnsData()
    {
        var result = _connection.Query("SELECT * FROM Users");
        Assert.NotNull(result);
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
```

**TUnit Equivalent (Option 1: Using IDisposable):**
```csharp
public class DatabaseTests : IDisposable
{
    private DatabaseConnection _connection = null!;

    public DatabaseTests()
    {
        _connection = new DatabaseConnection();
        _connection.Open();
    }

    [Test]
    public async Task Query_ReturnsData()
    {
        var result = _connection.Query("SELECT * FROM Users");
        await Assert.That(result).IsNotNull();
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
```

**TUnit Equivalent (Option 2: Using Hooks):**
```csharp
public class DatabaseTests
{
    private DatabaseConnection _connection = null!;

    [Before(Test)]
    public async Task Setup()
    {
        _connection = new DatabaseConnection();
        await _connection.OpenAsync();
    }

    [Test]
    public async Task Query_ReturnsData()
    {
        var result = _connection.Query("SELECT * FROM Users");
        await Assert.That(result).IsNotNull();
    }

    [After(Test)]
    public async Task Cleanup()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
    }
}
```

**Key Changes:**
- Constructor setup can remain, or use `[Before(Test)]`
- IDisposable can remain, or use `[After(Test)]`
- Hooks support async operations natively
- Multiple `[After(Test)]` methods are guaranteed to run even if one fails

#### IAsyncLifetime → Before/After Hooks

**xUnit Code:**
```csharp
public class AsyncSetupTests : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = new HttpClient();
        await _client.GetAsync("https://api.example.com/warm-up");
    }

    [Fact]
    public async Task FetchData_ReturnsSuccess()
    {
        var response = await _client.GetAsync("https://api.example.com/data");
        Assert.True(response.IsSuccessStatusCode);
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await Task.CompletedTask;
    }
}
```

**TUnit Equivalent:**
```csharp
public class AsyncSetupTests
{
    private HttpClient _client = null!;

    [Before(Test)]
    public async Task Setup()
    {
        _client = new HttpClient();
        await _client.GetAsync("https://api.example.com/warm-up");
    }

    [Test]
    public async Task FetchData_ReturnsSuccess()
    {
        var response = await _client.GetAsync("https://api.example.com/data");
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }

    [After(Test)]
    public async Task Cleanup()
    {
        _client?.Dispose();
    }
}
```

**Key Changes:**
- `IAsyncLifetime.InitializeAsync()` → `[Before(Test)]`
- `IAsyncLifetime.DisposeAsync()` → `[After(Test)]`
- More explicit and easier to understand at a glance

### Shared Context and Fixtures

#### IClassFixture → ClassDataSource

**xUnit Code:**
```csharp
public class DatabaseFixture : IDisposable
{
    public DatabaseConnection Connection { get; }

    public DatabaseFixture()
    {
        Connection = new DatabaseConnection();
        Connection.Open();
    }

    public void Dispose()
    {
        Connection?.Close();
        Connection?.Dispose();
    }
}

public class UserRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public UserRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void GetUser_ReturnsUser()
    {
        var repo = new UserRepository(_fixture.Connection);
        var user = repo.GetUser(1);
        Assert.NotNull(user);
    }

    [Fact]
    public void GetAllUsers_ReturnsUsers()
    {
        var repo = new UserRepository(_fixture.Connection);
        var users = repo.GetAllUsers();
        Assert.NotEmpty(users);
    }
}
```

**TUnit Equivalent:**
```csharp
public class DatabaseFixture : IDisposable
{
    public DatabaseConnection Connection { get; }

    public DatabaseFixture()
    {
        Connection = new DatabaseConnection();
        Connection.Open();
    }

    public void Dispose()
    {
        Connection?.Close();
        Connection?.Dispose();
    }
}

[ClassDataSource<DatabaseFixture>(Shared = SharedType.PerClass)]
public class UserRepositoryTests(DatabaseFixture fixture)
{
    [Test]
    public async Task GetUser_ReturnsUser()
    {
        var repo = new UserRepository(fixture.Connection);
        var user = repo.GetUser(1);
        await Assert.That(user).IsNotNull();
    }

    [Test]
    public async Task GetAllUsers_ReturnsUsers()
    {
        var repo = new UserRepository(fixture.Connection);
        var users = repo.GetAllUsers();
        await Assert.That(users).IsNotEmpty();
    }
}
```

**Key Changes:**
- `IClassFixture<T>` interface → `[ClassDataSource<T>(Shared = SharedType.PerClass)]` attribute
- Fixture injected via primary constructor
- `Shared = SharedType.PerClass` ensures one instance per test class

#### Collection Fixtures → Shared ClassDataSource

**xUnit Code:**
```csharp
[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}

public class DatabaseFixture : IDisposable
{
    public DatabaseConnection Connection { get; }

    public DatabaseFixture()
    {
        Connection = new DatabaseConnection();
        Connection.Open();
    }

    public void Dispose() => Connection?.Dispose();
}

[Collection("Database collection")]
public class UserTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public UserTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void CreateUser_Succeeds()
    {
        // Test using _fixture.Connection
    }
}

[Collection("Database collection")]
public class ProductTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public ProductTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void CreateProduct_Succeeds()
    {
        // Test using _fixture.Connection
    }
}
```

**TUnit Equivalent:**
```csharp
public class DatabaseFixture : IDisposable
{
    public DatabaseConnection Connection { get; }

    public DatabaseFixture()
    {
        Connection = new DatabaseConnection();
        Connection.Open();
    }

    public void Dispose() => Connection?.Dispose();
}

[ClassDataSource<DatabaseFixture>(Shared = SharedType.Keyed, Key = "DatabaseCollection")]
public class UserTests(DatabaseFixture fixture)
{
    [Test]
    public async Task CreateUser_Succeeds()
    {
        // Test using fixture.Connection
    }
}

[ClassDataSource<DatabaseFixture>(Shared = SharedType.Keyed, Key = "DatabaseCollection")]
public class ProductTests(DatabaseFixture fixture)
{
    [Test]
    public async Task CreateProduct_Succeeds()
    {
        // Test using fixture.Connection
    }
}
```

**Key Changes:**
- `[Collection("name")]` → `[ClassDataSource<T>(Shared = SharedType.Keyed, Key = "name")]`
- No need for CollectionDefinition class
- All classes with same Key share the fixture instance

#### Assembly Fixture → ClassDataSource with PerAssembly

**xUnit doesn't have native assembly fixtures, but TUnit does:**

**TUnit Example:**
```csharp
public class ApplicationFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }

    public ApplicationFixture()
    {
        // Setup once for entire assembly
        ServiceProvider = ConfigureServices();
    }

    public void Dispose()
    {
        // Cleanup once after all tests
    }
}

[ClassDataSource<ApplicationFixture>(Shared = SharedType.PerAssembly)]
public class IntegrationTests(ApplicationFixture fixture)
{
    [Test]
    public async Task Test1()
    {
        var service = fixture.ServiceProvider.GetService<IMyService>();
        await Assert.That(service).IsNotNull();
    }
}
```

### Test Output

#### ITestOutputHelper → TestContext

**xUnit Code:**
```csharp
public class LoggingTests
{
    private readonly ITestOutputHelper _output;

    public LoggingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Test_WithLogging()
    {
        _output.WriteLine("Starting test");

        var result = PerformOperation();

        _output.WriteLine($"Result: {result}");
        Assert.True(result > 0);
    }
}
```

**TUnit Equivalent:**
```csharp
public class LoggingTests
{
    [Test]
    public async Task Test_WithLogging(TestContext context)
    {
        context.OutputWriter.WriteLine("Starting test");

        var result = PerformOperation();

        context.OutputWriter.WriteLine($"Result: {result}");
        await Assert.That(result).IsGreaterThan(0);
    }
}
```

**Key Changes:**
- `ITestOutputHelper` injected in constructor → `TestContext` injected as method parameter
- Access output via `context.OutputWriter.WriteLine()`
- TestContext provides additional test metadata

#### Test Attachments

xUnit v3 introduced test attachments. TUnit also supports this capability:

**xUnit v3 Code:**
```csharp
public class TestWithAttachments
{
    private readonly ITestContextAccessor _testContextAccessor;
    
    public TestWithAttachments(ITestContextAccessor testContextAccessor)
    {
        _testContextAccessor = testContextAccessor;
    }
    
    [Fact]
    public async Task Test_WithAttachment()
    {
        // Test logic
        var logPath = "test-log.txt";
        await File.WriteAllTextAsync(logPath, "test logs");
        
        _testContextAccessor.Current!.Attachments.Add(
            new FileAttachment(logPath, "Test Log"));
    }
}
```

**TUnit Equivalent:**
```csharp
public class TestWithAttachments
{
    [Test]
    public async Task Test_WithAttachment()
    {
        // Test logic
        var logPath = "test-log.txt";
        await File.WriteAllTextAsync(logPath, "test logs");
        
        TestContext.Current!.Output.AttachArtifact(new Artifact
        {
            File = new FileInfo(logPath),
            DisplayName = "Test Log",
            Description = "Logs captured during test execution"  // Optional
        });
    }
}
```

For more information about working with test artifacts, including session-level artifacts and best practices, see the [Test Artifacts guide](../test-lifecycle/artifacts.md).

### Traits and Categories

#### Trait → Property

**xUnit Code:**
```csharp
public class FeatureTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public void ImportantIntegrationTest()
    {
        // Test implementation
    }
}
```

**TUnit Equivalent:**
```csharp
public class FeatureTests
{
    [Test]
    [Property("Category", "Integration")]
    [Property("Priority", "High")]
    public async Task ImportantIntegrationTest()
    {
        // Test implementation
    }
}
```

**Key Changes:**
- `[Trait("key", "value")]` → `[Property("key", "value")]`
- Can be used for filtering: `--treenode-filter "/*/*/*/*[Category=Integration]"`

### Assertions

#### Basic Assertions

**xUnit Code:**
```csharp
[Fact]
public void Assertions_Examples()
{
    Assert.Equal(5, 2 + 3);
    Assert.NotEqual(5, 2 + 2);
    Assert.True(5 > 3);
    Assert.False(5 < 3);
    Assert.Null(null);
    Assert.NotNull("value");
    Assert.Same(obj1, obj2);
    Assert.NotSame(obj1, obj3);
}
```

**TUnit Equivalent:**
```csharp
[Test]
public async Task Assertions_Examples()
{
    await Assert.That(2 + 3).IsEqualTo(5);
    await Assert.That(2 + 2).IsNotEqualTo(5);
    await Assert.That(5 > 3).IsTrue();
    await Assert.That(5 < 3).IsFalse();
    await Assert.That((object?)null).IsNull();
    await Assert.That("value").IsNotNull();
    await Assert.That(obj1).IsSameReference(obj2);
    await Assert.That(obj1).IsNotSameReference(obj3);
}
```

#### Collection Assertions

**xUnit Code:**
```csharp
[Fact]
public void Collection_Assertions()
{
    var list = new[] { 1, 2, 3 };

    Assert.Contains(2, list);
    Assert.DoesNotContain(5, list);
    Assert.Empty(Array.Empty<int>());
    Assert.NotEmpty(list);
    Assert.Equal(3, list.Length);
}
```

**TUnit Equivalent:**
```csharp
[Test]
public async Task Collection_Assertions()
{
    var list = new[] { 1, 2, 3 };

    await Assert.That(list).Contains(2);
    await Assert.That(list).DoesNotContain(5);
    await Assert.That(Array.Empty<int>()).IsEmpty();
    await Assert.That(list).IsNotEmpty();
    await Assert.That(list).Count().IsEqualTo(3);
}
```

#### String Assertions

**xUnit Code:**
```csharp
[Fact]
public void String_Assertions()
{
    var text = "Hello, World!";

    Assert.Contains("World", text);
    Assert.DoesNotContain("xyz", text);
    Assert.StartsWith("Hello", text);
    Assert.EndsWith("!", text);
    Assert.Matches(@"H\w+", text);
}
```

**TUnit Equivalent:**
```csharp
[Test]
public async Task String_Assertions()
{
    var text = "Hello, World!";

    await Assert.That(text).Contains("World");
    await Assert.That(text).DoesNotContain("xyz");
    await Assert.That(text).StartsWith("Hello");
    await Assert.That(text).EndsWith("!");
    await Assert.That(text).Matches(@"H\w+");
}
```

#### Exception Assertions

**xUnit Code:**
```csharp
[Fact]
public void Exception_Assertions()
{
    Assert.Throws<ArgumentException>(() => ThrowsException());

    var ex = Assert.Throws<ArgumentException>(() => ThrowsException());
    Assert.Equal("paramName", ex.ParamName);
}

[Fact]
public async Task Async_Exception_Assertions()
{
    await Assert.ThrowsAsync<InvalidOperationException>(() => ThrowsExceptionAsync());
}
```

**TUnit Equivalent:**
```csharp
[Test]
public async Task Exception_Assertions()
{
    await Assert.ThrowsAsync<ArgumentException>(() => ThrowsException());

    var ex = await Assert.ThrowsAsync<ArgumentException>(() => ThrowsException());
    await Assert.That(ex.ParamName).IsEqualTo("paramName");
}

[Test]
public async Task Async_Exception_Assertions()
{
    await Assert.ThrowsAsync<InvalidOperationException>(() => ThrowsExceptionAsync());
}
```

**Key Changes:**
- Both sync and async use `Assert.ThrowsAsync` in TUnit
- Returned exception can be further asserted on

### Complete Example: Real-World Test Class

**xUnit Code:**
```csharp
public class UserServiceTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _dbFixture;
    private readonly ITestOutputHelper _output;
    private UserService _userService = null!;

    public UserServiceTests(DatabaseFixture dbFixture, ITestOutputHelper output)
    {
        _dbFixture = dbFixture;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _userService = new UserService(_dbFixture.Connection);
        await _userService.InitializeAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("john@example.com", "John")]
    [InlineData("jane@example.com", "Jane")]
    public async Task CreateUser_WithValidData_Succeeds(string email, string name)
    {
        _output.WriteLine($"Creating user: {name}");

        var user = await _userService.CreateUserAsync(email, name);

        Assert.NotNull(user);
        Assert.Equal(email, user.Email);
        Assert.Equal(name, user.Name);

        _output.WriteLine($"User created with ID: {user.Id}");
    }

    [Fact]
    public async Task GetUser_WhenNotFound_ThrowsException()
    {
        await Assert.ThrowsAsync<UserNotFoundException>(
            () => _userService.GetUserAsync(99999));
    }

    [Theory]
    [MemberData(nameof(GetInvalidEmails))]
    public async Task CreateUser_WithInvalidEmail_ThrowsException(string invalidEmail)
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _userService.CreateUserAsync(invalidEmail, "Test"));
    }

    public static IEnumerable<object[]> GetInvalidEmails()
    {
        yield return new object[] { "" };
        yield return new object[] { "not-an-email" };
        yield return new object[] { "@example.com" };
    }
}
```

**TUnit Equivalent:**
```csharp
[ClassDataSource<DatabaseFixture>(Shared = SharedType.PerClass)]
public class UserServiceTests(DatabaseFixture dbFixture)
{
    private UserService _userService = null!;

    [Before(Test)]
    public async Task Setup()
    {
        _userService = new UserService(dbFixture.Connection);
        await _userService.InitializeAsync();
    }

    [Test]
    [Arguments("john@example.com", "John")]
    [Arguments("jane@example.com", "Jane")]
    public async Task CreateUser_WithValidData_Succeeds(string email, string name, TestContext context)
    {
        context.OutputWriter.WriteLine($"Creating user: {name}");

        var user = await _userService.CreateUserAsync(email, name);

        await Assert.That(user).IsNotNull();
        await Assert.That(user.Email).IsEqualTo(email);
        await Assert.That(user.Name).IsEqualTo(name);

        context.OutputWriter.WriteLine($"User created with ID: {user.Id}");
    }

    [Test]
    public async Task GetUser_WhenNotFound_ThrowsException()
    {
        await Assert.ThrowsAsync<UserNotFoundException>(
            () => _userService.GetUserAsync(99999));
    }

    [Test]
    [MethodDataSource(nameof(GetInvalidEmails))]
    public async Task CreateUser_WithInvalidEmail_ThrowsException(string invalidEmail)
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _userService.CreateUserAsync(invalidEmail, "Test"));
    }

    public static IEnumerable<string> GetInvalidEmails()
    {
        yield return "";
        yield return "not-an-email";
        yield return "@example.com";
    }
}
```

**Key Differences Summary:**
- Class-level fixtures use attributes instead of interfaces
- Setup/teardown use `[Before]`/`[After]` attributes instead of IAsyncLifetime
- Primary constructor for fixture injection
- TestContext injected as method parameter when needed
- All tests are async by default
- Data sources return strongly-typed values (not object[])
- Fluent assertion syntax

## Code Coverage

### Important: Coverlet is Not Compatible with TUnit

If you're using **Coverlet** (`coverlet.collector` or `coverlet.msbuild`) for code coverage in your xUnit projects, you'll need to migrate to **Microsoft.Testing.Extensions.CodeCoverage**.

**Why?** TUnit uses the modern `Microsoft.Testing.Platform` instead of VSTest, and Coverlet only works with the legacy VSTest platform.

### Good News: Coverage is Built In! 🎉

When you install the **TUnit** meta package, it automatically includes `Microsoft.Testing.Extensions.CodeCoverage` for you. You don't need to install it separately!

### Migration Steps

#### 1. Remove Coverlet Packages

Remove any Coverlet packages from your project file:

**Remove these lines from your `.csproj`:**
```xml
<!-- Remove these -->
<PackageReference Include="coverlet.collector" Version="x.x.x" />
<PackageReference Include="coverlet.msbuild" Version="x.x.x" />
```

#### 2. Verify TUnit Meta Package

Ensure you're using the **TUnit** meta package (not just TUnit.Core):

**Your `.csproj` should have:**
```xml
<PackageReference Include="TUnit" Version="0.x.x" />
```

This automatically brings in:
- `Microsoft.Testing.Extensions.CodeCoverage` (coverage support)
- `Microsoft.Testing.Extensions.TrxReport` (test result reports)

#### 3. Update Your Coverage Commands

Replace your old Coverlet commands with the new Microsoft coverage syntax:

**Old (Coverlet with xUnit):**
```bash
# With coverlet.collector
dotnet test --collect:"XPlat Code Coverage"

# With coverlet.msbuild
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

**New (TUnit with Microsoft Coverage):**
```bash
# Run tests with coverage
dotnet run --configuration Release --coverage

# Specify output location
dotnet run --configuration Release --coverage --coverage-output ./coverage/

# Specify coverage format (default is cobertura)
dotnet run --configuration Release --coverage --coverage-output-format cobertura

# Multiple formats
dotnet run --configuration Release --coverage --coverage-output-format cobertura --coverage-output-format xml
```

#### 4. Update CI/CD Pipelines

If you have CI/CD pipelines that reference Coverlet, update them to use the new commands:

**GitHub Actions Example:**
```yaml
# Old (xUnit with Coverlet)
- name: Run tests with coverage
  run: dotnet test --collect:"XPlat Code Coverage"

# New (TUnit with Microsoft Coverage)
- name: Run tests with coverage
  run: dotnet run --project ./tests/MyProject.Tests --configuration Release --coverage
```

**Azure Pipelines Example:**
```yaml
# Old (xUnit with Coverlet)
- task: DotNetCoreCLI@2
  inputs:
    command: 'test'
    arguments: '--collect:"XPlat Code Coverage"'

# New (TUnit with Microsoft Coverage)
- task: DotNetCoreCLI@2
  inputs:
    command: 'run'
    arguments: '--configuration Release --coverage --coverage-output $(Agent.TempDirectory)/coverage/'
```

### Coverage Output Formats

The Microsoft coverage tool supports multiple output formats:

```bash
# Cobertura (default, widely supported)
dotnet run --configuration Release --coverage --coverage-output-format cobertura

# XML (Visual Studio format)
dotnet run --configuration Release --coverage --coverage-output-format xml

# Cobertura + XML
dotnet run --configuration Release --coverage \
  --coverage-output-format cobertura \
  --coverage-output-format xml
```

### Viewing Coverage Results

Coverage files are generated in your test output directory:

```
TestResults/
  ├── coverage.cobertura.xml
  └── <guid>/
      └── coverage.xml
```

You can view these with:
- **Visual Studio** - Built-in coverage viewer
- **VS Code** - Extensions like "Coverage Gutters"
- **ReportGenerator** - Generate HTML reports: `reportgenerator -reports:coverage.cobertura.xml -targetdir:coveragereport`
- **CI Tools** - Most CI systems can parse Cobertura format natively

### Advanced Coverage Configuration

You can customize coverage behavior with a `.runsettings` file:

**coverage.runsettings:**
```xml
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
              </Include>
              <Exclude>
                <ModulePath>.*tests\.dll$</ModulePath>
              </Exclude>
            </ModulePaths>
          </CodeCoverage>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

**Use it:**
```bash
dotnet run --configuration Release --coverage --coverage-settings coverage.runsettings
```

### Troubleshooting

**Coverage files not generated?**
- Ensure you're using the TUnit meta package, not just TUnit.Engine
- Verify you have a recent .NET SDK installed

**Missing coverage for some assemblies?**
- Use a `.runsettings` file to explicitly include/exclude modules
- See [Microsoft's documentation](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage)

**Need help?**
- See [TUnit Code Coverage Documentation](../extensions/extensions.md#code-coverage)
- Check [Microsoft's Code Coverage Guide](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage)
