# Migrating from MSTest

## Using TUnit's Code Fixers

TUnit has code fixers to help automate the migration from MSTest to TUnit.

These code fixers will handle most common scenarios, but you'll likely still need to do some manual adjustments. If you encounter issues or have suggestions for improvements, please raise an issue.

### Steps

#### Install the TUnit packages to your test projects
Use your IDE or the dotnet CLI to add the TUnit packages to your test projects

#### Remove the automatically added global usings
In your csproj add:

```xml
    <PropertyGroup>
        <TUnitImplicitUsings>false</TUnitImplicitUsings>
        <TUnitAssertionsImplicitUsings>false</TUnitAssertionsImplicitUsings>
    </PropertyGroup>
```

This is temporary - Just to make sure no types clash, and so the code fixers can distinguish between MSTest and TUnit types with similar names.

#### Rebuild the project
This ensures the TUnit packages have been restored and the analyzers should be loaded.

#### Run the code fixer via the dotnet CLI

`dotnet format analyzers --severity info --diagnostics TUMS0001`

#### Revert step `Remove the automatically added global usings`

#### Perform any manual bits that are still necessary
Review the converted code and make any necessary manual adjustments.
Raise an issue if you think something could be automated.

#### Remove the MSTest packages
Simply uninstall them once you've migrated

#### Done! (Hopefully)

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
        context.OutputWriter.WriteLine($"Test: {context.TestDetails.TestName}");
        context.OutputWriter.WriteLine($"Test ID: {context.TestDetails.TestId}");

        // Accessing test details
        context.OutputWriter.WriteLine($"Class: {context.TestDetails.ClassType.Name}");
        context.OutputWriter.WriteLine($"Method: {context.TestDetails.MethodInfo.Name}");

        // Accessing attributes and properties
        var properties = context.TestDetails.Attributes.OfType<PropertyAttribute>();
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
        var browserProp = context.TestDetails.Attributes
            .OfType<PropertyAttribute>()
            .FirstOrDefault(p => p.Key == "Browser");

        var envProp = context.TestDetails.Attributes
            .OfType<PropertyAttribute>()
            .FirstOrDefault(p => p.Key == "Environment");

        context.OutputWriter.WriteLine($"Running on {browserProp?.Value} in {envProp?.Value}");
    }
}
```

**Key Changes:**
- TestContext is injected as parameter, not a property
- Access output via `context.OutputWriter.WriteLine()`
- Test metadata available via `context.TestDetails`
- Properties accessed through attributes rather than dictionary
- More type-safe property access

## Key Differences to Note

1. **Async by Default**: TUnit tests and assertions are async by default. Add `async Task` to your test methods and `await` assertions.

2. **No TestClass Required**: TUnit doesn't require a `[TestClass]` attribute on test classes.

3. **Fluent Assertions**: TUnit uses a fluent assertion style with `Assert.That()` as the starting point.

4. **TestContext Changes**:
   - TestContext is injected as a parameter rather than a property
   - ClassInitialize and AssemblyInitialize don't receive TestContext parameters
   - Access metadata via `context.TestDetails` instead of various TestContext properties

5. **Dependency Injection**: TUnit has built-in support for dependency injection in test classes and methods.

6. **Hooks Instead of Initialize/Cleanup**: TUnit uses `[Before]` and `[After]` attributes with `HookType` to specify when they run.

7. **Static Class-Level Hooks**: Class-level and assembly-level setup/teardown methods must be static in TUnit.

8. **No DeploymentItem**: Configure file copying in your .csproj instead of using `[DeploymentItem]`.

9. **Property-Based Metadata**: Use `[Property("key", "value")]` for all metadata (Owner, Priority, Category, custom properties).