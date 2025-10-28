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

## Key Differences to Note

1. **Async by Default**: TUnit tests and assertions are async by default. Add `async Task` to your test methods and `await` assertions.

2. **No TestFixture Required**: TUnit doesn't require a `[TestFixture]` attribute on test classes.

3. **Fluent Assertions**: TUnit uses a fluent assertion style with `Assert.That()` as the starting point.

4. **Dependency Injection**: TUnit has built-in support for dependency injection in test classes and methods.

5. **Hooks Instead of Setup/Teardown**: TUnit uses `[Before]` and `[After]` attributes with `HookType` to specify when they run.

6. **TestContext Injection**: Instead of a static `TestContext`, TUnit injects it as a parameter where needed.