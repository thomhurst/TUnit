# Migrating from NUnit

## Using TUnit's Code Fixers

TUnit has code fixers to help automate the migration from NUnit to TUnit.

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

This is temporary - Just to make sure no types clash, and so the code fixers can distinguish between NUnit and TUnit types with similar names.

#### Rebuild the project
This ensures the TUnit packages have been restored and the analyzers should be loaded.

#### Run the code fixer via the dotnet CLI

`dotnet format analyzers --severity info --diagnostics TUNU0001`

#### Revert step `Remove the automatically added global usings`

#### Perform any manual bits that are still necessary
Review the converted code and make any necessary manual adjustments.
Raise an issue if you think something could be automated.

#### Remove the NUnit packages
Simply uninstall them once you've migrated

#### Done! (Hopefully)

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
await Assert.That(collection).HasCount().EqualTo(5);
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
    [Matrix([1, 2, 3], ["a", "b"])]
    public async Task TestCombinations(int x, string y)
    {
        await Assert.That(x).IsGreaterThan(0);
        await Assert.That(y).IsNotNull();
    }
}
```

**Key Changes:**
- `[Values(...)]` attributes on parameters → `[Matrix(...)]` attribute on method
- All combinations are automatically generated
- Cleaner syntax with collection expressions

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
    context.OutputWriter.WriteLine($"Test Name: {context.TestDetails.TestName}");
    context.OutputWriter.WriteLine($"Test ID: {context.TestDetails.TestId}");
    context.OutputWriter.WriteLine($"Class Name: {context.TestDetails.ClassType.Name}");

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
    await Assert.That(list).HasCount().EqualTo(5);
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