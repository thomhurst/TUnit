# Migrating from NUnit

:::from-nunit Performance Boost
Migrating from NUnit to TUnit can improve test execution speed. Check the [benchmarks](/docs/benchmarks) to see how TUnit compares.
:::

## Quick Reference

| NUnit | TUnit |
|-------|-------|
| `[TestFixture]` | *(remove - not needed)* |
| `[Test]` | `[Test]` |
| `[TestCase(...)]` | `[Arguments(...)]` |
| `[TestCaseSource(nameof(...))]` | `[MethodDataSource(nameof(...))]` |
| `[Category("value")]` | `[Category("value")]` *(same)* or `[Property("Category", "value")]` |
| `[Ignore]` | `[Skip]` |
| `[Explicit]` | `[Explicit]` |
| `[SetUp]` | `[Before(Test)]` |
| `[TearDown]` | `[After(Test)]` |
| `[OneTimeSetUp]` | `[Before(Class)]` |
| `[OneTimeTearDown]` | `[After(Class)]` |
| `[SetUpFixture]` + `[OneTimeSetUp]` | `[Before(Assembly)]` on static method |
| `[Values(...)]` on parameter | `[Matrix(...)]` on each parameter |
| `Assert.AreEqual(expected, actual)` | `await Assert.That(actual).IsEqualTo(expected)` |
| `Assert.That(actual, Is.EqualTo(expected))` | `await Assert.That(actual).IsEqualTo(expected)` |
| `Assert.Throws<T>(() => ...)` | `await Assert.ThrowsAsync<T>(() => ...)` |
| `TestContext.WriteLine(...)` | `TestContext` parameter with `context.OutputWriter.WriteLine(...)` |
| `TestContext.AddTestAttachment(path, name)` | `TestContext.Current!.Output.AttachArtifact(new Artifact { File = new FileInfo(path), DisplayName = name })` |
| `CollectionAssert.AreEqual(expected, actual)` | `await Assert.That(actual).IsEquivalentTo(expected)` |
| `StringAssert.Contains(substring, text)` | `await Assert.That(text).Contains(substring)` |

## Automated Migration with Code Fixers

TUnit includes Roslyn analyzers and code fixers that automate most of the migration work. The `TUNU0001` diagnostic identifies NUnit code patterns and provides automatic fixes to convert them to TUnit equivalents.

**What gets converted automatically:**
- `[TestFixture]` → removed (not needed in TUnit)
- `[Test]` → `[Test]` (stays the same)
- `[TestCase(...)]` → `[Arguments(...)]`
- `[TestCaseSource(nameof(...))]` → `[MethodDataSource(nameof(...))]`
- `[SetUp]` → `[Before(Test)]`
- `[TearDown]` → `[After(Test)]`
- `[OneTimeSetUp]` → `[Before(Class)]`
- `[OneTimeTearDown]` → `[After(Class)]`
- `[Ignore]` → `[Skip]`
- `[Category("...")]` → `[Property("Category", "...")]`
- Classic assertions: `Assert.AreEqual(expected, actual)` → `await Assert.That(actual).IsEqualTo(expected)`
- Constraint assertions: `Assert.That(actual, Is.EqualTo(expected))` → `await Assert.That(actual).IsEqualTo(expected)`
- Test methods converted to `async Task` with `await` on assertions

The code fixer handles roughly 80-90% of typical test suites automatically.

**What requires manual adjustment:**
- Custom `[TestCaseSource]` return types (convert `object[]` to tuples)
- Complex async patterns or custom awaitable types
- Custom fixtures or test base classes
- `[SetUpFixture]` with namespace-scoped setup (convert to assembly hooks)
- `TestContext.CurrentContext` static access (inject `TestContext` as parameter instead)
- `Assert.Multiple` blocks (use assertion groups or multiple awaited assertions)

If you find a common pattern that should be automated but isn't, please [open an issue](https://github.com/thomhurst/TUnit/issues).

### Prerequisites

- .NET SDK 8.0 or later (for `dotnet format` with analyzer support)
- TUnit packages installed in your test project

### Step-by-Step Migration

:::tip Safety First
Commit your changes or create a backup before running the code fixer. This allows you to review changes and revert if needed.
:::

**1. Install TUnit packages**

Add the TUnit packages to your test project alongside NUnit (temporarily):

```bash
dotnet add package TUnit
```

**2. Disable TUnit's implicit usings (temporary)**

Add these properties to your `.csproj` to prevent type name conflicts between NUnit and TUnit:

```xml
<PropertyGroup>
    <TUnitImplicitUsings>false</TUnitImplicitUsings>
    <TUnitAssertionsImplicitUsings>false</TUnitAssertionsImplicitUsings>
</PropertyGroup>
```

This allows the code fixer to distinguish between `NUnit.Framework.Assert` and `TUnit.Assertions.Assert`.

**3. Rebuild the project**

```bash
dotnet build
```

This restores packages and loads the TUnit analyzers. You should see `TUNU0001` warnings in your build output for NUnit code that can be converted.

**4. Run the automated code fixer**

```bash
dotnet format analyzers --severity info --diagnostics TUNU0001
```

This command applies all available fixes for the `TUNU0001` diagnostic. You'll see output indicating which files were modified.

:::warning Multi-targeting Projects
If your project targets multiple .NET versions (e.g., `net8.0;net9.0;net10.0`), you **must** specify a single target framework when running the code fixer. Multi-targeting can cause the code fixer to crash with the error `Changes must be within bounds of SourceText` due to a limitation in Roslyn's linked file handling.

**Option 1:** Specify a single framework via command line:
```bash
dotnet format analyzers --severity info --diagnostics TUNU0001 --framework net10.0
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
- Add `using TUnit.Core;` and `using TUnit.Assertions;` if not using implicit usings
- Convert data source methods to return tuples instead of `object[]`
- Replace `TestContext.CurrentContext` with injected `TestContext` parameter
- Update any custom assertion extensions

**7. Remove NUnit packages**

Once everything compiles and tests pass:

```bash
dotnet remove package NUnit
dotnet remove package NUnit3TestAdapter
```

**8. Verify the migration**

```bash
dotnet build
dotnet run -- --list-tests
```

### Troubleshooting

**Code fixer doesn't run / no files changed:**
- Ensure you rebuilt after adding TUnit packages
- Check that `TUNU0001` warnings appear in build output
- Try running with verbose output: `dotnet format analyzers --severity info --diagnostics TUNU0001 --verbosity detailed`

**Build errors after running code fixer:**
- Missing `await` keywords: ensure test methods are `async Task`
- Ambiguous `Assert`: remove NUnit usings or fully qualify types
- Type mismatch in data sources: convert `object[]` returns to tuples

**Analyzers not loading:**
- Verify TUnit package is installed: `dotnet list package`
- Try cleaning and rebuilding: `dotnet clean && dotnet build`

## Manual Migration Guide

### Test Attributes

`[TestFixture]` - Remove this attribute (not needed in TUnit)

`[Test]` remains `[Test]`

`[TestCase]` becomes `[Arguments]`

`[TestCaseSource]` becomes `[MethodDataSource]`

`[Category]` becomes `[Property("Category", "value")]`

`[Ignore]` becomes `[Skip]`

`[Explicit]` becomes `[Explicit]`

### Setup and Teardown

`[SetUp]` becomes `[Before(HookType.Test)]`

`[TearDown]` becomes `[After(HookType.Test)]`

`[OneTimeSetUp]` becomes `[Before(HookType.Class)]`

`[OneTimeTearDown]` becomes `[After(HookType.Class)]`

### Assertions

#### Classic Assertions
```csharp
// NUnit
Assert.AreEqual(expected, actual);
Assert.IsTrue(condition);
Assert.IsNull(value);
Assert.Greater(value1, value2);

// TUnit
await Assert.That(actual).IsEqualTo(expected);
await Assert.That(condition).IsTrue();
await Assert.That(value).IsNull();
await Assert.That(value1).IsGreaterThan(value2);
```

#### Constraint-Based Assertions
```csharp
// NUnit
Assert.That(actual, Is.EqualTo(expected));
Assert.That(value, Is.True);
Assert.That(value, Is.Null);
Assert.That(text, Does.Contain("substring"));
Assert.That(collection, Has.Count.EqualTo(5));

// TUnit
await Assert.That(actual).IsEqualTo(expected);
await Assert.That(value).IsTrue();
await Assert.That(value).IsNull();
await Assert.That(text).Contains("substring");
await Assert.That(collection).Count().IsEqualTo(5);
```

### Collection Assertions

```csharp
// NUnit
CollectionAssert.AreEqual(expected, actual);
CollectionAssert.Contains(collection, item);
CollectionAssert.IsEmpty(collection);

// TUnit
await Assert.That(actual).IsEquivalentTo(expected);
await Assert.That(collection).Contains(item);
await Assert.That(collection).IsEmpty();
```

### String Assertions

```csharp
// NUnit
StringAssert.Contains(substring, text);
StringAssert.StartsWith(prefix, text);
StringAssert.EndsWith(suffix, text);

// TUnit
await Assert.That(text).Contains(substring);
await Assert.That(text).StartsWith(prefix);
await Assert.That(text).EndsWith(suffix);
```

### Exception Testing

```csharp
// NUnit
Assert.Throws<InvalidOperationException>(() => DoSomething());
Assert.ThrowsAsync<InvalidOperationException>(async () => await DoSomethingAsync());

// TUnit
await Assert.ThrowsAsync<InvalidOperationException>(() => DoSomething());
await Assert.ThrowsAsync<InvalidOperationException>(async () => await DoSomethingAsync());
```

### Test Data Sources

#### TestCaseSource
```csharp
// NUnit
[TestCaseSource(nameof(TestData))]
public void TestMethod(int value, string text)
{
    // Test implementation
}

private static IEnumerable TestData()
{
    yield return new object[] { 1, "one" };
    yield return new object[] { 2, "two" };
}

// TUnit
[MethodDataSource(nameof(TestData))]
public async Task TestMethod(int value, string text)
{
    // Test implementation
}

private static IEnumerable<(int, string)> TestData()
{
    yield return (1, "one");
    yield return (2, "two");
}
```

### Parameterized Tests

```csharp
// NUnit
[TestCase(1, 2, 3)]
[TestCase(10, 20, 30)]
public void AdditionTest(int a, int b, int expected)
{
    Assert.AreEqual(expected, a + b);
}

// TUnit
[Test]
[Arguments(1, 2, 3)]
[Arguments(10, 20, 30)]
public async Task AdditionTest(int a, int b, int expected)
{
    await Assert.That(a + b).IsEqualTo(expected);
}
```

### Test Output

```csharp
// NUnit
TestContext.WriteLine("Test output");
TestContext.Out.WriteLine("More output");

// TUnit (inject TestContext)
public async Task MyTest(TestContext context)
{
    context.OutputWriter.WriteLine("Test output");
    context.OutputWriter.WriteLine("More output");
}
```

### Test Attachments

```csharp
// NUnit
[Test]
public void TestWithAttachment()
{
    // Test logic
    var logPath = "test-log.txt";
    File.WriteAllText(logPath, "test logs");
    
    TestContext.AddTestAttachment(logPath, "Test Log");
}

// TUnit
[Test]
public async Task TestWithAttachment()
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
```

For more information about working with test artifacts, including session-level artifacts and best practices, see the [Test Artifacts guide](../test-lifecycle/artifacts.md).

### Combinatorial Testing

#### Values and Combinatorial → Matrix

**NUnit Code:**
```csharp
public class CombinationTests
{
    [Test]
    public void TestCombinations(
        [Values(1, 2, 3)] int x,
        [Values("a", "b")] string y)
    {
        Assert.That(x, Is.GreaterThan(0));
        Assert.That(y, Is.Not.Null);
    }
}
```

**TUnit Equivalent:**
```csharp
public class CombinationTests
{
    [Test]
    public async Task TestCombinations(
        [Matrix(1, 2, 3)] int x,
        [Matrix("a", "b")] string y)
    {
        await Assert.That(x).IsGreaterThan(0);
        await Assert.That(y).IsNotNull();
    }
}
```

**Key Changes:**
- `[Values(...)]` attributes on parameters → `[Matrix(...)]` attributes on parameters
- All combinations are automatically generated (3 × 2 = 6 test cases)
- Each parameter gets its own `[Matrix]` attribute with the values to test

### Test Fixture with Parameters

#### Parameterized TestFixture

**NUnit Code:**
```csharp
[TestFixture("Development")]
[TestFixture("Staging")]
[TestFixture("Production")]
public class EnvironmentTests
{
    private readonly string _environment;

    public EnvironmentTests(string environment)
    {
        _environment = environment;
    }

    [Test]
    public void ConfigurationIsValid()
    {
        var config = LoadConfiguration(_environment);
        Assert.That(config, Is.Not.Null);
        Assert.That(config.IsValid, Is.True);
    }
}
```

**TUnit Equivalent:**
```csharp
[Arguments("Development")]
[Arguments("Staging")]
[Arguments("Production")]
public class EnvironmentTests(string environment)
{
    [Test]
    public async Task ConfigurationIsValid()
    {
        var config = LoadConfiguration(environment);
        await Assert.That(config).IsNotNull();
        await Assert.That(config.IsValid).IsTrue();
    }
}
```

**Key Changes:**
- `[TestFixture(...)]` with parameters → `[Arguments(...)]` on the class
- Primary constructor for cleaner syntax
- All tests in the class are repeated for each argument set

### Complete Test Class Example

**NUnit Code:**
```csharp
[TestFixture]
public class ProductServiceTests
{
    private IDatabase _database;
    private ProductService _productService;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // Runs once before all tests in the class
        _database = new InMemoryDatabase();
        _database.Initialize();
    }

    [SetUp]
    public void Setup()
    {
        // Runs before each test
        _productService = new ProductService(_database);
    }

    [Test]
    [Category("Unit")]
    [TestCase("Widget", 10.99)]
    [TestCase("Gadget", 25.50)]
    public void CreateProduct_WithValidData_Succeeds(string name, decimal price)
    {
        var product = _productService.CreateProduct(name, price);

        Assert.That(product, Is.Not.Null);
        Assert.That(product.Name, Is.EqualTo(name));
        Assert.That(product.Price, Is.EqualTo(price));
    }

    [Test]
    [Category("Unit")]
    public void GetProduct_WhenNotFound_ReturnsNull()
    {
        var product = _productService.GetProduct(999);
        Assert.That(product, Is.Null);
    }

    [Test]
    [TestCaseSource(nameof(InvalidProductData))]
    public void CreateProduct_WithInvalidData_ThrowsException(string name, decimal price)
    {
        Assert.Throws<ArgumentException>(() => _productService.CreateProduct(name, price));
    }

    private static IEnumerable InvalidProductData()
    {
        yield return new object[] { "", 10.00 };
        yield return new object[] { "Product", -5.00 };
        yield return new object[] { null, 10.00 };
    }

    [TearDown]
    public void TearDown()
    {
        // Runs after each test
        _productService?.Dispose();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        // Runs once after all tests in the class
        _database?.Dispose();
    }
}
```

**TUnit Equivalent:**
```csharp
public class ProductServiceTests
{
    private IDatabase _database = null!;
    private ProductService _productService = null!;

    [Before(Class)]
    public async Task ClassSetup()
    {
        // Runs once before all tests in the class
        _database = new InMemoryDatabase();
        await _database.InitializeAsync();
    }

    [Before(Test)]
    public async Task Setup()
    {
        // Runs before each test
        _productService = new ProductService(_database);
    }

    [Test]
    [Property("Category", "Unit")]
    [Arguments("Widget", 10.99)]
    [Arguments("Gadget", 25.50)]
    public async Task CreateProduct_WithValidData_Succeeds(string name, decimal price)
    {
        var product = _productService.CreateProduct(name, price);

        await Assert.That(product).IsNotNull();
        await Assert.That(product.Name).IsEqualTo(name);
        await Assert.That(product.Price).IsEqualTo(price);
    }

    [Test]
    [Property("Category", "Unit")]
    public async Task GetProduct_WhenNotFound_ReturnsNull()
    {
        var product = _productService.GetProduct(999);
        await Assert.That(product).IsNull();
    }

    [Test]
    [MethodDataSource(nameof(InvalidProductData))]
    public async Task CreateProduct_WithInvalidData_ThrowsException(string name, decimal price)
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _productService.CreateProduct(name, price));
    }

    private static IEnumerable<(string name, decimal price)> InvalidProductData()
    {
        yield return ("", 10.00m);
        yield return ("Product", -5.00m);
        yield return (null!, 10.00m);
    }

    [After(Test)]
    public async Task Cleanup()
    {
        // Runs after each test
        _productService?.Dispose();
    }

    [After(Class)]
    public async Task ClassCleanup()
    {
        // Runs once after all tests in the class
        _database?.Dispose();
    }
}
```

**Key Changes:**
- `[TestFixture]` attribute removed (not needed)
- `[OneTimeSetUp]` → `[Before(Class)]` (and can be async)
- `[SetUp]` → `[Before(Test)]`
- `[TearDown]` → `[After(Test)]`
- `[OneTimeTearDown]` → `[After(Class)]`
- `[TestCase(...)]` → `[Arguments(...)]`
- Data sources return tuples instead of `object[]`
- All assertions are awaited

### Range Testing

**NUnit Code:**
```csharp
[Test]
public void ProcessValue_WithRange([Range(1, 10)] int value)
{
    var result = ProcessValue(value);
    Assert.That(result, Is.GreaterThan(0));
}
```

**TUnit Equivalent:**
```csharp
[Test]
[MethodDataSource(nameof(GetRange))]
public async Task ProcessValue_WithRange(int value)
{
    var result = ProcessValue(value);
    await Assert.That(result).IsGreaterThan(0);
}

private static IEnumerable<int> GetRange()
{
    return Enumerable.Range(1, 10);
}
```

### Custom Test Context Properties

**NUnit Code:**
```csharp
[Test]
public void Test_WithContextProperties()
{
    TestContext.WriteLine($"Test Name: {TestContext.CurrentContext.Test.Name}");
    TestContext.WriteLine($"Test Status: {TestContext.CurrentContext.Result.Outcome.Status}");

    // Test implementation
}
```

**TUnit Equivalent:**
```csharp
[Test]
public async Task Test_WithContextProperties(TestContext context)
{
    context.OutputWriter.WriteLine($"Test Name: {context.Metadata.TestName}");
    context.OutputWriter.WriteLine($"Test ID: {context.Metadata.TestDetails.TestId}");
    context.OutputWriter.WriteLine($"Class Name: {context.Metadata.TestDetails.ClassType.Name}");

    // Test implementation
}
```

### Assertion Constraint Mapping

**NUnit Code:**
```csharp
[Test]
public void ComplexAssertions()
{
    var value = 42;
    var text = "Hello World";
    var list = new[] { 1, 2, 3, 4, 5 };

    // Comparison assertions
    Assert.That(value, Is.EqualTo(42));
    Assert.That(value, Is.Not.EqualTo(0));
    Assert.That(value, Is.GreaterThan(40));
    Assert.That(value, Is.LessThanOrEqualTo(50));
    Assert.That(value, Is.InRange(40, 45));

    // String assertions
    Assert.That(text, Does.StartWith("Hello"));
    Assert.That(text, Does.EndWith("World"));
    Assert.That(text, Does.Contain("llo Wor"));
    Assert.That(text, Does.Match(@"^Hello"));

    // Collection assertions
    Assert.That(list, Has.Count.EqualTo(5));
    Assert.That(list, Has.Member(3));
    Assert.That(list, Has.All.GreaterThan(0));
    Assert.That(list, Is.Ordered);

    // Compound assertions
    Assert.That(value, Is.GreaterThan(40).And.LessThan(50));
    Assert.That(text, Is.Not.Null.And.Not.Empty);
}
```

**TUnit Equivalent:**
```csharp
[Test]
public async Task ComplexAssertions()
{
    var value = 42;
    var text = "Hello World";
    var list = new[] { 1, 2, 3, 4, 5 };

    // Comparison assertions
    await Assert.That(value).IsEqualTo(42);
    await Assert.That(value).IsNotEqualTo(0);
    await Assert.That(value).IsGreaterThan(40);
    await Assert.That(value).IsLessThanOrEqualTo(50);
    await Assert.That(value).IsBetween(40, 45);

    // String assertions
    await Assert.That(text).StartsWith("Hello");
    await Assert.That(text).EndsWith("World");
    await Assert.That(text).Contains("llo Wor");
    await Assert.That(text).Matches(@"^Hello");

    // Collection assertions
    await Assert.That(list).Count().IsEqualTo(5);
    await Assert.That(list).Contains(3);
    await Assert.That(list).AllSatisfy(x => x > 0);
    await Assert.That(list).IsInAscendingOrder();

    // Compound assertions (using And/Or)
    await Assert.That(value).IsGreaterThan(40).And.IsLessThan(50);
    await Assert.That(text).IsNotNull().And.IsNotEmpty();
}
```

### SetUpFixture for Assembly-Level Hooks

**NUnit Code:**
```csharp
[SetUpFixture]
public class AssemblySetup
{
    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        // Initialize resources needed by all tests
        Console.WriteLine("Assembly setup running");
    }

    [OneTimeTearDown]
    public void RunAfterAllTests()
    {
        // Cleanup resources
        Console.WriteLine("Assembly cleanup running");
    }
}
```

**TUnit Equivalent:**
```csharp
public static class AssemblyHooks
{
    [Before(Assembly)]
    public static async Task AssemblySetup()
    {
        // Initialize resources needed by all tests
        Console.WriteLine("Assembly setup running");
    }

    [After(Assembly)]
    public static async Task AssemblyCleanup()
    {
        // Cleanup resources
        Console.WriteLine("Assembly cleanup running");
    }
}
```

**Key Changes:**
- `[SetUpFixture]` → simple static class
- `[OneTimeSetUp]` → `[Before(Assembly)]`
- `[OneTimeTearDown]` → `[After(Assembly)]`
- Methods must be static
- Can be async

## Key Differences to Note

1. **Async by Default**: TUnit tests and assertions are async by default. Add `async Task` to your test methods and `await` assertions.

2. **No TestFixture Required**: TUnit doesn't require a `[TestFixture]` attribute on test classes.

3. **Fluent Assertions**: TUnit uses a fluent assertion style with `Assert.That()` as the starting point.

4. **Dependency Injection**: TUnit has built-in support for dependency injection in test classes and methods.

5. **Hooks Instead of Setup/Teardown**: TUnit uses `[Before]` and `[After]` attributes with `HookType` to specify when they run.

6. **TestContext Injection**: Instead of a static `TestContext`, TUnit injects it as a parameter where needed.

7. **Isolated Test Instances**: Each test runs in its own class instance (NUnit's default behavior can be different).

## Code Coverage

### Important: Coverlet is Not Compatible with TUnit

If you're using **Coverlet** (`coverlet.collector` or `coverlet.msbuild`) for code coverage in your NUnit projects, you'll need to migrate to **Microsoft.Testing.Extensions.CodeCoverage**.

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

**Old (Coverlet with NUnit):**
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
# Old (NUnit with Coverlet)
- name: Run tests with coverage
  run: dotnet test --collect:"XPlat Code Coverage"

# New (TUnit with Microsoft Coverage)
- name: Run tests with coverage
  run: dotnet run --project ./tests/MyProject.Tests --configuration Release --coverage
```

**Azure Pipelines Example:**
```yaml
# Old (NUnit with Coverlet)
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

You can customize coverage behavior with a `testconfig.json` file:

**testconfig.json:**
```json
{
  "codeCoverage": {
    "Configuration": {
      "CodeCoverage": {
        "ModulePaths": {
          "Include": [".*\\.dll$"],
          "Exclude": [".*tests\\.dll$"]
        }
      }
    }
  }
}
```

Place the `testconfig.json` file in the same directory as your test project. It will be picked up automatically when running tests.

**Alternatively, you can use an XML coverage settings file:**
```bash
dotnet run --configuration Release --coverage --coverage-settings coverage.config
```

### Troubleshooting

**Coverage files not generated?**
- Ensure you're using the TUnit meta package, not just TUnit.Engine
- Verify you have a recent .NET SDK installed

**Missing coverage for some assemblies?**
- Use a `testconfig.json` file to explicitly include/exclude modules
- See [Microsoft's documentation](https://github.com/microsoft/codecoverage/blob/main/docs/configuration.md)

**Need help?**
- See [TUnit Code Coverage Documentation](../extensions/extensions.md#code-coverage)
- Check [Microsoft's Code Coverage Guide](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage)