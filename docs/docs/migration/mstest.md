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

## Key Differences to Note

1. **Async by Default**: TUnit tests and assertions are async by default. Add `async Task` to your test methods and `await` assertions.

2. **No TestClass Required**: TUnit doesn't require a `[TestClass]` attribute on test classes.

3. **Fluent Assertions**: TUnit uses a fluent assertion style with `Assert.That()` as the starting point.

4. **TestContext Changes**: 
   - TestContext is injected as a parameter rather than a property
   - ClassInitialize and AssemblyInitialize don't receive TestContext parameters

5. **Dependency Injection**: TUnit has built-in support for dependency injection in test classes and methods.

6. **Hooks Instead of Initialize/Cleanup**: TUnit uses `[Before]` and `[After]` attributes with `HookType` to specify when they run.

7. **Static Class-Level Hooks**: Class-level setup and teardown methods should be static in TUnit.