# Migrating from MSTest

:::from-mstest Performance Boost
Migrating from MSTest to TUnit can improve test execution speed. Check the [benchmarks](/docs/benchmarks) to see how TUnit compares.
:::

## Quick Reference

| MSTest | TUnit |
|--------|-------|
| `[TestClass]` | *(remove - not needed)* |
| `[TestMethod]` | `[Test]` |
| `[DataRow(...)]` | `[Arguments(...)]` |
| `[DynamicData(nameof(...), ...)]` | `[MethodDataSource(nameof(...))]` |
| `[TestCategory("value")]` | `[Property("Category", "value")]` |
| `[Ignore]` | `[Skip]` |
| `[Priority(n)]` | `[Property("Priority", "n")]` |
| `[Owner("value")]` | `[Property("Owner", "value")]` |
| `[TestInitialize]` | `[Before(Test)]` |
| `[TestCleanup]` | `[After(Test)]` |
| `[ClassInitialize]` | `[Before(Class)]` *(remove TestContext parameter)* |
| `[ClassCleanup]` | `[After(Class)]` |
| `[AssemblyInitialize]` | `[Before(Assembly)]` *(remove TestContext parameter)* |
| `[AssemblyCleanup]` | `[After(Assembly)]` |
| `[Timeout(ms)]` | `[Timeout(ms)]` |
| `[DataTestMethod]` | `[Test]` |
| `public TestContext TestContext { get; set; }` | `TestContext` method parameter |
| `Assert.AreEqual(expected, actual)` | `await Assert.That(actual).IsEqualTo(expected)` |
| `Assert.IsTrue(condition)` | `await Assert.That(condition).IsTrue()` |
| `Assert.IsNull(value)` | `await Assert.That(value).IsNull()` |
| `Assert.ThrowsException<T>(() => ...)` | `await Assert.ThrowsAsync<T>(() => ...)` |
| `Assert.Inconclusive("reason")` | `Skip.Test("reason")` |
| `CollectionAssert.Contains(collection, item)` | `await Assert.That(collection).Contains(item)` |
| `StringAssert.Contains(text, substring)` | `await Assert.That(text).Contains(substring)` |
| `Assert.AreSame(expected, actual)` | `await Assert.That(actual).IsSameReference(expected)` |

## Automated Migration with Code Fixers

TUnit includes Roslyn analyzers and code fixers that automate most of the migration work. The `TUMS0001` diagnostic identifies MSTest code patterns and provides automatic fixes to convert them to TUnit equivalents.

**What gets converted automatically:**
- `[TestClass]` → removed (not needed in TUnit)
- `[TestMethod]` → `[Test]`
- `[DataTestMethod]` → `[Test]`
- `[DataRow(...)]` → `[Arguments(...)]`
- `[DynamicData(nameof(...), ...)]` → `[MethodDataSource(nameof(...))]`
- `[TestInitialize]` → `[Before(Test)]`
- `[TestCleanup]` → `[After(Test)]`
- `[ClassInitialize]` → `[Before(Class)]` (removes `TestContext` parameter)
- `[ClassCleanup]` → `[After(Class)]`
- `[AssemblyInitialize]` → `[Before(Assembly)]` (removes `TestContext` parameter)
- `[AssemblyCleanup]` → `[After(Assembly)]`
- `[Ignore]` → `[Skip]`
- `[TestCategory("...")]` → `[Property("Category", "...")]`
- `[Priority(n)]` → `[Property("Priority", "n")]`
- `[Owner("...")]` → `[Property("Owner", "...")]`
- `Assert.AreEqual(expected, actual)` → `await Assert.That(actual).IsEqualTo(expected)`
- `Assert.IsTrue(condition)` → `await Assert.That(condition).IsTrue()`
- `Assert.ThrowsException<T>(...)` → `await Assert.ThrowsAsync<T>(...)`
- Test methods converted to `async Task` with `await` on assertions

The code fixer handles roughly 80-90% of typical test suites automatically.

**What requires manual adjustment:**
- `public TestContext TestContext { get; set; }` property → inject `TestContext` as method parameter instead
- `[ClassInitialize]` / `[AssemblyInitialize]` methods that use the `TestContext` parameter
- Custom `[DynamicData]` return types (convert `IEnumerable<object[]>` to `IEnumerable<(...)>` tuples)
- `[DeploymentItem]` attributes → configure file copying in `.csproj` instead
- `Assert.Inconclusive("...")` → `Skip.Test("...")`
- `[ExpectedException]` attribute (deprecated) → `await Assert.ThrowsAsync<T>(...)`
- Complex `TestContext` property access patterns

If you find a common pattern that should be automated but isn't, please [open an issue](https://github.com/thomhurst/TUnit/issues).

### Prerequisites

- .NET SDK 8.0 or later (for `dotnet format` with analyzer support)
- TUnit packages installed in your test project

### Step-by-Step Migration

:::tip Safety First
Commit your changes or create a backup before running the code fixer. This allows you to review changes and revert if needed.
:::

**1. Install TUnit packages**

Add the TUnit packages to your test project alongside MSTest (temporarily):

```bash
dotnet add package TUnit
```

**2. Disable TUnit's implicit usings (temporary)**

Add these properties to your `.csproj` to prevent type name conflicts between MSTest and TUnit:

```xml
<PropertyGroup>
    <TUnitImplicitUsings>false</TUnitImplicitUsings>
    <TUnitAssertionsImplicitUsings>false</TUnitAssertionsImplicitUsings>
</PropertyGroup>
```

This allows the code fixer to distinguish between `Microsoft.VisualStudio.TestTools.UnitTesting.Assert` and `TUnit.Assertions.Assert`.

**3. Rebuild the project**

```bash
dotnet build
```

This restores packages and loads the TUnit analyzers. You should see `TUMS0001` warnings in your build output for MSTest code that can be converted.

**4. Run the automated code fixer**

```bash
dotnet format analyzers --severity info --diagnostics TUMS0001
```

This command applies all available fixes for the `TUMS0001` diagnostic. You'll see output indicating which files were modified.

:::warning Multi-targeting Projects
If your project targets multiple .NET versions (e.g., `net8.0;net9.0;net10.0`), you **must** specify a single target framework when running the code fixer. Multi-targeting can cause the code fixer to crash with the error `Changes must be within bounds of SourceText` due to a limitation in Roslyn's linked file handling.

**Option 1:** Specify a single framework via command line:
```bash
dotnet format analyzers --severity info --diagnostics TUMS0001 --framework net10.0
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
- Replace `public TestContext TestContext { get; set; }` with `TestContext` method parameter
- Remove `TestContext` parameter from `[ClassInitialize]` and `[AssemblyInitialize]` methods
- Convert data source methods to return tuples instead of `object[]`
- Replace `[DeploymentItem]` with `.csproj` file copy configuration
- Replace `Assert.Inconclusive(...)` with `Skip.Test(...)`
- Add `using TUnit.Core;` and `using TUnit.Assertions;` if not using implicit usings

**7. Remove MSTest packages**

Once everything compiles and tests pass:

```bash
dotnet remove package MSTest.TestFramework
dotnet remove package MSTest.TestAdapter
```

**8. Verify the migration**

```bash
dotnet build
dotnet run -- --list-tests
```

### Troubleshooting

**Code fixer doesn't run / no files changed:**
- Ensure you rebuilt after adding TUnit packages
- Check that `TUMS0001` warnings appear in build output
- Try running with verbose output: `dotnet format analyzers --severity info --diagnostics TUMS0001 --verbosity detailed`

**Build errors after running code fixer:**
- Missing `await` keywords: ensure test methods are `async Task`
- Ambiguous `Assert`: remove MSTest usings or fully qualify types
- Type mismatch in data sources: convert `IEnumerable<object[]>` returns to `IEnumerable<(...)>` tuples

**TestContext errors:**
- Remove the `public TestContext TestContext { get; set; }` property
- Add `TestContext context` parameter to test methods that need it
- Access output via `context.OutputWriter.WriteLine(...)` instead of `TestContext.WriteLine(...)`

**ClassInitialize/AssemblyInitialize errors:**
- Remove the `TestContext context` parameter from these methods
- If you need test context in setup, use `[Before(Test)]` instead which can receive `TestContext`

**Analyzers not loading:**
- Verify TUnit package is installed: `dotnet list package`
- Try cleaning and rebuilding: `dotnet clean && dotnet build`

## Manual Migration Guide

### Test Attributes

`[TestClass]` - Remove this attribute (not needed in TUnit)

`[TestMethod]` becomes `[Test]`

`[DataRow]` becomes `[Arguments]`

`[DynamicData]` becomes `[MethodDataSource]`

`[TestCategory]` becomes `[Property("Category", "value")]`

`[Ignore]` becomes `[Skip]`

`[Priority]` becomes `[Property("Priority", "value")]`

`[Owner]` becomes `[Property("Owner", "value")]`

### Setup and Teardown

`[TestInitialize]` becomes `[Before(HookType.Test)]`

`[TestCleanup]` becomes `[After(HookType.Test)]`

`[ClassInitialize]` becomes `[Before(HookType.Class)]` and remove the TestContext parameter

`[ClassCleanup]` becomes `[After(HookType.Class)]`

`[AssemblyInitialize]` becomes `[Before(HookType.Assembly)]` and remove the TestContext parameter

`[AssemblyCleanup]` becomes `[After(HookType.Assembly)]`

### Assertions

#### Basic Assertions
```csharp
// MSTest
Assert.AreEqual(expected, actual);
Assert.AreNotEqual(expected, actual);
Assert.IsTrue(condition);
Assert.IsFalse(condition);
Assert.IsNull(value);
Assert.IsNotNull(value);

// TUnit
await Assert.That(actual).IsEqualTo(expected);
await Assert.That(actual).IsNotEqualTo(expected);
await Assert.That(condition).IsTrue();
await Assert.That(condition).IsFalse();
await Assert.That(value).IsNull();
await Assert.That(value).IsNotNull();
```

#### Reference Assertions
```csharp
// MSTest
Assert.AreSame(expected, actual);
Assert.AreNotSame(expected, actual);

// TUnit
await Assert.That(actual).IsSameReference(expected);
await Assert.That(actual).IsNotSameReference(expected);
```

#### Type Assertions
```csharp
// MSTest
Assert.IsInstanceOfType(value, typeof(string));
Assert.IsNotInstanceOfType(value, typeof(int));

// TUnit
await Assert.That(value).IsAssignableTo<string>();
await Assert.That(value).IsNotAssignableTo<int>();
```

### Collection Assertions

```csharp
// MSTest
CollectionAssert.AreEqual(expected, actual);
CollectionAssert.AreNotEqual(expected, actual);
CollectionAssert.Contains(collection, item);
CollectionAssert.DoesNotContain(collection, item);
CollectionAssert.AllItemsAreNotNull(collection);

// TUnit
await Assert.That(actual).IsEquivalentTo(expected);
await Assert.That(actual).IsNotEquivalentTo(expected);
await Assert.That(collection).Contains(item);
await Assert.That(collection).DoesNotContain(item);
await Assert.That(collection).AllSatisfy(x => x != null);
```

### String Assertions

```csharp
// MSTest
StringAssert.Contains(text, substring);
StringAssert.StartsWith(text, prefix);
StringAssert.EndsWith(text, suffix);
StringAssert.Matches(text, pattern);

// TUnit
await Assert.That(text).Contains(substring);
await Assert.That(text).StartsWith(prefix);
await Assert.That(text).EndsWith(suffix);
await Assert.That(text).Matches(pattern);
```

### Exception Testing

```csharp
// MSTest
Assert.ThrowsException<InvalidOperationException>(() => DoSomething());
await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => DoSomethingAsync());

// TUnit
await Assert.ThrowsAsync<InvalidOperationException>(() => DoSomething());
await Assert.ThrowsAsync<InvalidOperationException>(() => DoSomethingAsync());
```

### Test Data Sources

#### DataRow
```csharp
// MSTest
[TestMethod]
[DataRow(1, 2, 3)]
[DataRow(10, 20, 30)]
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

#### DynamicData
```csharp
// MSTest
[TestMethod]
[DynamicData(nameof(TestData), DynamicDataSourceType.Method)]
public void TestMethod(int value, string text)
{
    // Test implementation
}

private static IEnumerable<object[]> TestData()
{
    yield return new object[] { 1, "one" };
    yield return new object[] { 2, "two" };
}

// TUnit
[Test]
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

### TestContext Usage

```csharp
// MSTest
[TestClass]
public class MyTests
{
    public TestContext TestContext { get; set; }
    
    [TestMethod]
    public void MyTest()
    {
        TestContext.WriteLine("Test output");
    }
    
    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        // Setup code
    }
}

// TUnit
public class MyTests
{
    [Test]
    public async Task MyTest(TestContext context)
    {
        context.OutputWriter.WriteLine("Test output");
    }
    
    [Before(HookType.Class)]
    public static async Task ClassInit()
    {
        // Setup code - no TestContext parameter needed
    }
}
```

### Test Attachments

```csharp
// MSTest
[TestMethod]
public void TestWithAttachment()
{
    // Test logic
    var logPath = "test-log.txt";
    File.WriteAllText(logPath, "test logs");
    
    TestContext.AddResultFile(logPath);
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

### Assert.Fail

```csharp
// MSTest
Assert.Fail("Test failed with reason");

// TUnit
Assert.Fail("Test failed with reason");
```

### Inconclusive Tests

```csharp
// MSTest
Assert.Inconclusive("Test is inconclusive");

// TUnit
Skip.Test("Test is inconclusive");
```

### Complete Test Class Transformation

**MSTest Code:**
```csharp
[TestClass]
public class OrderServiceTests
{
    private static IDatabase _sharedDatabase;
    private IOrderService _orderService;

    public TestContext TestContext { get; set; }

    [AssemblyInitialize]
    public static void AssemblyInit(TestContext context)
    {
        // Runs once per assembly
        Console.WriteLine("Assembly initialization");
    }

    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        // Runs once per test class
        _sharedDatabase = new InMemoryDatabase();
        _sharedDatabase.Initialize();
    }

    [TestInitialize]
    public void TestInit()
    {
        // Runs before each test
        _orderService = new OrderService(_sharedDatabase);
        TestContext.WriteLine("Starting test");
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(1, "Product A", 10.00)]
    [DataRow(2, "Product B", 20.00)]
    public void CreateOrder_WithValidData_Succeeds(int productId, string productName, double price)
    {
        var order = _orderService.CreateOrder(productId, productName, (decimal)price);

        Assert.IsNotNull(order);
        Assert.AreEqual(productId, order.ProductId);
        Assert.AreEqual(productName, order.ProductName);
        Assert.AreEqual((decimal)price, order.Price);

        TestContext.WriteLine($"Order created: {order.Id}");
    }

    [TestMethod]
    [DynamicData(nameof(GetInvalidOrders), DynamicDataSourceType.Method)]
    public void CreateOrder_WithInvalidData_ThrowsException(int productId, string productName, double price)
    {
        Assert.ThrowsException<ArgumentException>(() =>
            _orderService.CreateOrder(productId, productName, (decimal)price));
    }

    private static IEnumerable<object[]> GetInvalidOrders()
    {
        yield return new object[] { 0, "Product", 10.00 };
        yield return new object[] { 1, "", 10.00 };
        yield return new object[] { 1, "Product", -5.00 };
    }

    [TestMethod]
    [Ignore("Not implemented yet")]
    public void CancelOrder_ValidOrder_Succeeds()
    {
        // Test implementation
    }

    [TestCleanup]
    public void TestCleanup()
    {
        // Runs after each test
        _orderService?.Dispose();
        TestContext.WriteLine("Test completed");
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        // Runs once after all tests in class
        _sharedDatabase?.Dispose();
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        // Runs once after all tests in assembly
        Console.WriteLine("Assembly cleanup");
    }
}
```

**TUnit Equivalent:**
```csharp
public class OrderServiceTests
{
    private static IDatabase _sharedDatabase = null!;
    private IOrderService _orderService = null!;

    [Before(Assembly)]
    public static async Task AssemblyInit()
    {
        // Runs once per assembly
        Console.WriteLine("Assembly initialization");
    }

    [Before(Class)]
    public static async Task ClassInit()
    {
        // Runs once per test class
        _sharedDatabase = new InMemoryDatabase();
        await _sharedDatabase.InitializeAsync();
    }

    [Before(Test)]
    public async Task TestInit(TestContext context)
    {
        // Runs before each test
        _orderService = new OrderService(_sharedDatabase);
        context.OutputWriter.WriteLine("Starting test");
    }

    [Test]
    [Property("Category", "Unit")]
    [Arguments(1, "Product A", 10.00)]
    [Arguments(2, "Product B", 20.00)]
    public async Task CreateOrder_WithValidData_Succeeds(int productId, string productName, double price, TestContext context)
    {
        var order = _orderService.CreateOrder(productId, productName, (decimal)price);

        await Assert.That(order).IsNotNull();
        await Assert.That(order.ProductId).IsEqualTo(productId);
        await Assert.That(order.ProductName).IsEqualTo(productName);
        await Assert.That(order.Price).IsEqualTo((decimal)price);

        context.OutputWriter.WriteLine($"Order created: {order.Id}");
    }

    [Test]
    [MethodDataSource(nameof(GetInvalidOrders))]
    public async Task CreateOrder_WithInvalidData_ThrowsException(int productId, string productName, double price)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _orderService.CreateOrder(productId, productName, (decimal)price));
    }

    private static IEnumerable<(int productId, string productName, double price)> GetInvalidOrders()
    {
        yield return (0, "Product", 10.00);
        yield return (1, "", 10.00);
        yield return (1, "Product", -5.00);
    }

    [Test]
    [Skip("Not implemented yet")]
    public async Task CancelOrder_ValidOrder_Succeeds()
    {
        // Test implementation
    }

    [After(Test)]
    public async Task TestCleanup(TestContext context)
    {
        // Runs after each test
        _orderService?.Dispose();
        context.OutputWriter.WriteLine("Test completed");
    }

    [After(Class)]
    public static async Task ClassCleanup()
    {
        // Runs once after all tests in class
        _sharedDatabase?.Dispose();
    }

    [After(Assembly)]
    public static async Task AssemblyCleanup()
    {
        // Runs once after all tests in assembly
        Console.WriteLine("Assembly cleanup");
    }
}
```

**Key Changes:**
- `[TestClass]` removed (not required in TUnit)
- `TestContext` property → injected as method parameter when needed
- `[AssemblyInitialize]` → `[Before(Assembly)]` (no TestContext parameter)
- `[ClassInitialize]` → `[Before(Class)]` (no TestContext parameter)
- `[TestInitialize]` → `[Before(Test)]`
- `[TestCleanup]` → `[After(Test)]`
- `[ClassCleanup]` → `[After(Class)]`
- `[AssemblyCleanup]` → `[After(Assembly)]`
- `[TestMethod]` → `[Test]`
- `[DataRow(...)]` → `[Arguments(...)]`
- `[DynamicData(...)]` → `[MethodDataSource(...)]`
- Data sources return tuples instead of `object[]`
- All lifecycle methods can be async
- All assertions are awaited

### DataTestMethod with Multiple Sources

**MSTest Code:**
```csharp
[TestClass]
public class CalculatorTests
{
    [DataTestMethod]
    [DataRow(2, 3, 5)]
    [DataRow(10, 15, 25)]
    [DataRow(-5, 5, 0)]
    public void Add_ValidNumbers_ReturnsSum(int a, int b, int expected)
    {
        var calculator = new Calculator();
        var result = calculator.Add(a, b);
        Assert.AreEqual(expected, result);
    }

    [DataTestMethod]
    [DynamicData(nameof(GetMultiplicationData), DynamicDataSourceType.Method)]
    public void Multiply_ValidNumbers_ReturnsProduct(int a, int b, int expected)
    {
        var calculator = new Calculator();
        var result = calculator.Multiply(a, b);
        Assert.AreEqual(expected, result);
    }

    private static IEnumerable<object[]> GetMultiplicationData()
    {
        yield return new object[] { 2, 3, 6 };
        yield return new object[] { 4, 5, 20 };
        yield return new object[] { 0, 10, 0 };
    }
}
```

**TUnit Equivalent:**
```csharp
public class CalculatorTests
{
    [Test]
    [Arguments(2, 3, 5)]
    [Arguments(10, 15, 25)]
    [Arguments(-5, 5, 0)]
    public async Task Add_ValidNumbers_ReturnsSum(int a, int b, int expected)
    {
        var calculator = new Calculator();
        var result = calculator.Add(a, b);
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [MethodDataSource(nameof(GetMultiplicationData))]
    public async Task Multiply_ValidNumbers_ReturnsProduct(int a, int b, int expected)
    {
        var calculator = new Calculator();
        var result = calculator.Multiply(a, b);
        await Assert.That(result).IsEqualTo(expected);
    }

    private static IEnumerable<(int a, int b, int expected)> GetMultiplicationData()
    {
        yield return (2, 3, 6);
        yield return (4, 5, 20);
        yield return (0, 10, 0);
    }
}
```

### Timeout Handling

**MSTest Code:**
```csharp
[TestClass]
public class TimeoutTests
{
    [TestMethod]
    [Timeout(5000)] // 5 seconds
    public async Task LongRunningOperation_CompletesInTime()
    {
        await Task.Delay(2000);
        Assert.IsTrue(true);
    }
}
```

**TUnit Equivalent:**
```csharp
public class TimeoutTests
{
    [Test]
    [Timeout(5000)] // 5 seconds in milliseconds
    public async Task LongRunningOperation_CompletesInTime()
    {
        await Task.Delay(2000);
        await Assert.That(true).IsTrue();
    }
}
```

**Key Changes:**
- Timeout attribute works similarly in both frameworks
- TUnit's timeout is also in milliseconds

### Expected Exception (Obsolete Pattern)

**MSTest Code (Old Style):**
```csharp
[TestClass]
public class ValidationTests
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ValidateInput_NullInput_ThrowsException()
    {
        Validator.ValidateInput(null);
    }
}
```

**TUnit Equivalent:**
```csharp
public class ValidationTests
{
    [Test]
    public async Task ValidateInput_NullInput_ThrowsException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Validator.ValidateInput(null));
    }
}
```

**Key Changes:**
- `[ExpectedException]` is obsolete even in MSTest
- Use `Assert.ThrowsAsync` for better control and assertions
- Can capture and assert on the thrown exception

### DeploymentItem Pattern

**MSTest Code:**
```csharp
[TestClass]
[DeploymentItem("testdata.json")]
public class FileBasedTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void LoadTestData_ValidFile_Succeeds()
    {
        var filePath = Path.Combine(TestContext.DeploymentDirectory, "testdata.json");
        var data = File.ReadAllText(filePath);
        Assert.IsNotNull(data);
    }
}
```

**TUnit Equivalent:**
```csharp
public class FileBasedTests
{
    [Test]
    public async Task LoadTestData_ValidFile_Succeeds()
    {
        // TUnit doesn't have DeploymentItem - use relative paths or copy files in build
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "testdata.json");
        var data = await File.ReadAllTextAsync(filePath);
        await Assert.That(data).IsNotNull();
    }
}
```

**Key Changes:**
- TUnit doesn't have `[DeploymentItem]`
- Configure file copying in your .csproj instead:
```xml
<ItemGroup>
  <None Update="testdata.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### Owner and Priority Properties

**MSTest Code:**
```csharp
[TestClass]
public class FeatureTests
{
    [TestMethod]
    [Owner("John Doe")]
    [Priority(1)]
    [TestCategory("Critical")]
    public void CriticalFeature_WorksCorrectly()
    {
        // Test implementation
    }

    [TestMethod]
    [Owner("Jane Smith")]
    [Priority(3)]
    [TestCategory("Enhancement")]
    public void Enhancement_WorksCorrectly()
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
    [Property("Owner", "John Doe")]
    [Property("Priority", "1")]
    [Property("Category", "Critical")]
    public async Task CriticalFeature_WorksCorrectly()
    {
        // Test implementation
    }

    [Test]
    [Property("Owner", "Jane Smith")]
    [Property("Priority", "3")]
    [Property("Category", "Enhancement")]
    public async Task Enhancement_WorksCorrectly()
    {
        // Test implementation
    }
}
```

**Key Changes:**
- `[Owner("value")]` → `[Property("Owner", "value")]`
- `[Priority(n)]` → `[Property("Priority", "n")]`
- `[TestCategory("value")]` → `[Property("Category", "value")]`
- Can filter by properties: `--treenode-filter "/*/*/*/*[Priority=1]"`

### Advanced Assertions Comparison

**MSTest Code:**
```csharp
[TestMethod]
public void AdvancedAssertions_Examples()
{
    var value = 42;
    var text = "Hello, World!";
    var list = new List<int> { 1, 2, 3 };
    var person = new Person { Name = "John", Age = 30 };

    // Numeric assertions
    Assert.AreEqual(42, value);
    Assert.AreNotEqual(0, value);

    // String assertions with custom messages
    StringAssert.Contains(text, "World", "Should contain 'World'");
    StringAssert.StartsWith(text, "Hello");
    StringAssert.EndsWith(text, "!");
    StringAssert.Matches(text, new Regex(@"^\w+"));

    // Collection assertions
    CollectionAssert.Contains(list, 2);
    CollectionAssert.DoesNotContain(list, 5);
    CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list);
    CollectionAssert.AreEquivalent(new[] { 3, 1, 2 }, list);
    CollectionAssert.AllItemsAreUnique(list);

    // Conditional assertions
    if (value > 40)
    {
        Assert.Inconclusive("Value is too high for this test");
    }

    Assert.Fail("Intentional failure for demonstration");
}
```

**TUnit Equivalent:**
```csharp
[Test]
public async Task AdvancedAssertions_Examples()
{
    var value = 42;
    var text = "Hello, World!";
    var list = new List<int> { 1, 2, 3 };
    var person = new Person { Name = "John", Age = 30 };

    // Numeric assertions
    await Assert.That(value).IsEqualTo(42);
    await Assert.That(value).IsNotEqualTo(0);

    // String assertions with custom messages
    await Assert.That(text).Contains("World").WithMessage("Should contain 'World'");
    await Assert.That(text).StartsWith("Hello");
    await Assert.That(text).EndsWith("!");
    await Assert.That(text).Matches(@"^\w+");

    // Collection assertions
    await Assert.That(list).Contains(2);
    await Assert.That(list).DoesNotContain(5);
    await Assert.That(list).IsEquivalentTo(new[] { 1, 2, 3 });
    await Assert.That(list).IsEquivalentTo(new[] { 3, 1, 2 }); // Order doesn't matter
    await Assert.That(list).HasDistinctItems();

    // Conditional test skipping
    if (value > 40)
    {
        Skip.Test("Value is too high for this test");
    }

    Assert.Fail("Intentional failure for demonstration");
}
```

### TestContext Usage Patterns

**MSTest Code:**
```csharp
[TestClass]
public class ContextTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void UsingTestContext_AllProperties()
    {
        // Writing output
        TestContext.WriteLine($"Test: {TestContext.TestName}");
        TestContext.WriteLine($"Result: {TestContext.CurrentTestOutcome}");

        // Accessing test properties
        TestContext.WriteLine($"Fully Qualified Name: {TestContext.FullyQualifiedTestClassName}");

        // Custom properties
        TestContext.Properties["CustomKey"] = "CustomValue";
        var customValue = TestContext.Properties["CustomKey"];

        Assert.IsTrue(true);
    }

    [TestMethod]
    [TestProperty("Browser", "Chrome")]
    [TestProperty("Environment", "Staging")]
    public void TestWithProperties()
    {
        var browser = TestContext.Properties["Browser"];
        var environment = TestContext.Properties["Environment"];

        TestContext.WriteLine($"Running on {browser} in {environment}");
    }
}
```

**TUnit Equivalent:**
```csharp
public class ContextTests
{
    [Test]
    public async Task UsingTestContext_AllProperties(TestContext context)
    {
        // Writing output
        context.OutputWriter.WriteLine($"Test: {context.Metadata.TestName}");
        context.OutputWriter.WriteLine($"Test ID: {context.Metadata.TestDetails.TestId}");

        // Accessing test details
        context.OutputWriter.WriteLine($"Class: {context.Metadata.TestDetails.ClassType.Name}");
        context.OutputWriter.WriteLine($"Method: {context.Metadata.TestDetails.MethodInfo.Name}");

        // Accessing attributes and properties
        var properties = context.Metadata.TestDetails.Attributes.OfType<PropertyAttribute>();
        foreach (var prop in properties)
        {
            context.OutputWriter.WriteLine($"{prop.Key}: {prop.Value}");
        }

        await Assert.That(true).IsTrue();
    }

    [Test]
    [Property("Browser", "Chrome")]
    [Property("Environment", "Staging")]
    public async Task TestWithProperties(TestContext context)
    {
        var browserProp = context.Metadata.TestDetails.Attributes
            .OfType<PropertyAttribute>()
            .FirstOrDefault(p => p.Key == "Browser");

        var envProp = context.Metadata.TestDetails.Attributes
            .OfType<PropertyAttribute>()
            .FirstOrDefault(p => p.Key == "Environment");

        context.OutputWriter.WriteLine($"Running on {browserProp?.Value} in {envProp?.Value}");
    }
}
```

**Key Changes:**
- TestContext is injected as parameter, not a property
- Access output via `context.OutputWriter.WriteLine()`
- Test metadata available via `context.Metadata.TestDetails`
- Properties accessed through attributes rather than dictionary
- More type-safe property access

## Key Differences to Note

1. **Async by Default**: TUnit tests and assertions are async by default. Add `async Task` to your test methods and `await` assertions.

2. **No TestClass Required**: TUnit doesn't require a `[TestClass]` attribute on test classes.

3. **Fluent Assertions**: TUnit uses a fluent assertion style with `Assert.That()` as the starting point.

4. **TestContext Changes**:
   - TestContext is injected as a parameter rather than a property
   - ClassInitialize and AssemblyInitialize don't receive TestContext parameters
   - Access metadata via `context.Metadata.TestDetails` instead of various TestContext properties

5. **Dependency Injection**: TUnit has built-in support for dependency injection in test classes and methods.

6. **Hooks Instead of Initialize/Cleanup**: TUnit uses `[Before]` and `[After]` attributes with `HookType` to specify when they run.

7. **Static Class-Level Hooks**: Class-level and assembly-level setup/teardown methods must be static in TUnit.

8. **No DeploymentItem**: Configure file copying in your .csproj instead of using `[DeploymentItem]`.

9. **Property-Based Metadata**: Use `[Property("key", "value")]` for all metadata (Owner, Priority, Category, custom properties).

## Code Coverage

### Important: Coverlet is Not Compatible with TUnit

If you're using **Coverlet** (`coverlet.collector` or `coverlet.msbuild`) for code coverage in your MSTest projects, you'll need to migrate to **Microsoft.Testing.Extensions.CodeCoverage**.

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

**Old (Coverlet with MSTest):**
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
# Old (MSTest with Coverlet)
- name: Run tests with coverage
  run: dotnet test --collect:"XPlat Code Coverage"

# New (TUnit with Microsoft Coverage)
- name: Run tests with coverage
  run: dotnet run --project ./tests/MyProject.Tests --configuration Release --coverage
```

**Azure Pipelines Example:**
```yaml
# Old (MSTest with Coverlet)
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
- **Visual Studio** - Built-in coverage viewer (MSTest users will find this familiar)
- **VS Code** - Extensions like "Coverage Gutters"
- **ReportGenerator** - Generate HTML reports: `reportgenerator -reports:coverage.cobertura.xml -targetdir:coveragereport`
- **CI Tools** - Most CI systems can parse Cobertura format natively (same as before)

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