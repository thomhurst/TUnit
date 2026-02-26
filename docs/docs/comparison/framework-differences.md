# Framework Differences

TUnit is inspired by NUnit and xUnit, which are excellent frameworks that have served the .NET community well.

**Why use TUnit?**
TUnit aims to address some pain points and limitations found in other frameworks, especially around parallelism, lifecycle hooks, test isolation, and extensibility.
Below are some scenarios where TUnit offers a different or improved approach.

## Quick Comparison: Basic Test Structure

| Framework | Class Attribute | Method Attribute | Example |
|-----------|----------------|------------------|---------|
| **TUnit** | ❌ None needed | `[Test]` | `public class MyTests { [Test] public async Task MyTest() { } }` |
| **MSTest** | `[TestClass]` | `[TestMethod]` | `[TestClass] public class MyTests { [TestMethod] public void MyTest() { } }` |
| **NUnit** | `[TestFixture]` (optional) | `[Test]` | `[TestFixture] public class MyTests { [Test] public void MyTest() { } }` |
| **xUnit** | ❌ None needed | `[Fact]` or `[Theory]` | `public class MyTests { [Fact] public void MyTest() { } }` |

**Key Point**: TUnit does **NOT** require `[TestClass]` or `[TestFixture]` attributes. You only need `[Test]` on your test methods.

### Complete TUnit Test Example

**With explicit using statements:**

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTests;

public class ValidatorTests  // No [TestClass] needed!
{
    [Test]  // Only this attribute is required
    public async Task IsPositive_WithNegativeNumber_ReturnsFalse()
    {
        var result = Validator.IsPositive(-1);
        await Assert.That(result).IsFalse();
    }
}
```

**Without explicit using statements (TUnit automatically provides global usings):**

```csharp
namespace MyTests;

public class ValidatorTests
{
    [Test]
    public async Task IsPositive_WithNegativeNumber_ReturnsFalse()
    {
        var result = Validator.IsPositive(-1);
        await Assert.That(result).IsFalse();
    }
}
```

The TUnit package automatically configures global usings for common TUnit namespaces, so you don't need to include using statements in your test files.

The following sections describe specific limitations in other frameworks that TUnit addresses.

## xUnit

### Async tests parallel limit
xUnit gives you a way to limit the 'thread count' - but this doesn't map 1-to-1 to async tests. 1 thread can run multiple async tests when they yield, and that means limiting the thread count doesn't limit the test count. This can be problematic in certain scenarios. For example, running UI tests, you might want to limit the number of concurrent tests because spawning up too many browser instances overwhelms your system. With TUnit, you can pass in a CLI flag to limit the number of concurrent tests: `--maximum-parallel-tests 8`

### Set up and tear downs
Set ups and tear-downs work largely off of constructors and the `IDisposable` interface (TUnit can do this too if you like this pattern). If you have async requirements, you can implement an `IAsyncLifetime` interface. While some people like this approach as its familiar, things get messier when your classes rely on inheritance. If your base class uses these interfaces, you have to then hide the base members, implement your version, and then call the base method manually. Also with tear downs, if you want to guarantee execution of multiple pieces of code, you can't implement the interface multiple times. So you end up having to do lots of try/catches. In TUnit, you can declare multiple methods with `[After(Test)]` attributes, and they are all guaranteed to run, even if a previous one failed. And it'll lazily aggregate and throw any exceptions after they've all run. On top of this, any set ups and tear downs are collected all the way down to the base class, and run in an order than means members are initialised and cleaned up that makes sense:
    - For set ups, that means base set up methods are run first, and then the subsequent inherited class's methods
    - For clean ups, the top most clean ups are run first, and then the subsequent base methods

### Assembly level hooks
There isn't a simplistic way to do something on starting an assembly's tests. For example, we might want to spin up 1 in-memory server to run some tests against. TUnit supports this with a simple static class, with a method containing the attribute `[Before(Assembly)]`. Tear down is as simple as another method with `[After(Assembly)]`. 

### TestContext
Sometimes it is useful to access information about the state of a test. For example, when running UI tests, taking a screenshot on failure makes it easier to see what went wrong. xUnit does not have a native way of determining if a test failed when you're in a tear down method. With TUnit, you can inject in a `TestContext` object into your tear down method, or you can call the static `TestContext.Current` static method.

### Assertions
xUnit assertions are fairly basic and have the problem of it being unclear which argument goes in which position, without sifting through intellisense/documentation.

```csharp
var one = 2;
Assert.Equal(1, one)
Assert.Equal(one, 1)
```

## NUnit

### Shared test class instances
This is a common source of subtle bugs that many developers encounter without realizing it. The default behaviour of NUnit is to run all tests within a class against a single instance of that class. That means if state is stored in fields or properties, it persists from previous tests.
This pattern — sometimes called leaky test state — undermines test isolation. Tests should be independent and unable to affect one another. TUnit avoids this by design: every test runs against a new instance, with no way to opt out. To share state across tests, use `static` fields. This makes shared state explicit and immediately visible to anyone reading the code.

### Setting properties based off of dynamically injected data
Consider a multi-tenanted test suite where tests are repeated with different tenants injected in.
For example:
```csharp
[TestFixtureSource(typeof(Tenant), nameof(Tenant.AllTenants))]
public class MyTests(Tenant tenant)
{
    [Test]
    public async Task Test1()
    {
        ...
    }
}
```

A natural next step is to filter by tenant. In NUnit, a custom attribute with `IApplyToTest` can set a property based on the constructor argument, but this does not work because tests are enumerated on start-up before the fixture source provides its values. In TUnit, tests are enumerated and initialised via source generation, so this is all done up-front. A property can be set with an attribute implementing `ITestDiscoveryEvent` based on constructor arguments, and then filtered with `dotnet run --treenode-filter /*/*/*/*[Tenant=MyTenant]`.

### Assembly & class level attributes
Want to use the `[Repeat]` or `[Retry]` attributes on a class? Or even an assembly? You can't. They're only supported for test methods. With TUnit, most attributes are supported at Test, Class & Assembly levels. Test takes the highest priority, then class, then assembly. So you could set defaults with an assembly/class attribute, and then override it for certain tests by setting that same attribute on the test.

### Assertions
NUnit assertions largely influenced the way that TUnit assertions work. However, NUnit assertions do not have compile-time checks. Nothing prevents checking if a string is negative (`NUnitAssert.That("String", Is.Negative);`) or if a boolean throws an exception (`NUnitAssert.That(true, Throws.ArgumentException);`). These assertions do not make sense. There are analyzers to help catch these - But they will compile if these analyzers aren't run. TUnit assertions are built with the type system in mind (where possible!). Specific assertions are built via extensions to the relevant types, and not in a generic sense that could apply to anything. That means when you're using intellisense to see what methods you have available, you should only see assertions that are relevant for your type. This makes it harder to make mistakes, and decreases your feedback loop time.

## Other

### Source Generated + Native AOT + Single File Support
TUnit is source generated, so test discovery happens at compile time rather than through runtime reflection. You can inspect the generated code yourself. Because tests are source generated, you can build your test projects using Native AOT or as a Single File application — something that NUnit and xUnit do not currently support.

### More Lifecycle Hooks
TUnit provides a wide range of lifecycle hook points.
The attributes you can use on your hook methods are:
- `[Before(Test)]` - Run before every test in the class it's defined in
- `[After(Test)]` - Run after every test in the class it's defined in
- `[Before(Class)]` - Run once before all tests in the class it's defined in
- `[After(Class)]` - Run once after all tests in the class it's defined in
- `[Before(Assembly)]` - Run once before all tests in the assembly it's defined in
- `[After(Assembly)]` - Run once after all tests in the assembly it's defined in
- `[Before(TestSession)]` - Run once before all tests in the test run
- `[After(TestSession)]` - Run once after all tests in the test run
- `[Before(TestDiscovery)]` - Run once before any tests are discovered
- `[After(TestDiscovery)]` - Run once after all tests are discovered

- `[BeforeEvery(Test)]` - Run before every test in the test run
- `[AfterEvery(Test)]` - Run after every test in the test run
- `[BeforeEvery(Class)]` - Run before the first test in every class in the test run
- `[AfterEvery(Class)]` - Run after the last test in every class in the test run
- `[BeforeEvery(Assembly)]` - Run before the first test in every assembly in the test run
- `[AfterEvery(Assembly)]` - Run after the last test in every assembly in the test run

All hooks accept a relevant `[HookType]Context` object, giving you access to information about the current test run.


### Test Dependencies
In other frameworks, running tests in a specific order usually requires turning off parallelisation and setting an `[Order]` attribute with 1, 2, 3, etc.
In TUnit, you can use a `[DependsOn(...)]` attribute. That test will wait to start until its dependencies have finished, without disabling parallelisation for other tests.

```csharp
    [Test]
    public async Task Test1()
    {
        ...
    }
    
    [Test]
    public async Task Test2()
    {
        ...
    }
    
    [Test]
    [DependsOn(nameof(Test1))]
    [DependsOn(nameof(Test2))]
    public async Task Test3()
    {
        ...
    }
```

### Class Arguments
Many data injection mechanisms in xUnit/NUnit work for either the method or the class, but not both. With TUnit, you can use `[Arguments(...)]`, `[Matrix(...)]`, `[MethodDataSource(...)]`, and other data attributes on both classes and test methods.
