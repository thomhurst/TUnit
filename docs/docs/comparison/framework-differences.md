---
sidebar_position: 2
---

# Framework Differences

TUnit is inspired by NUnit and xUnit, and first and foremost I want to say that these are amazing frameworks and no hate to them.
So you'll be asking why use TUnit instead of them, right?
Here are some things I've stumbled across in the past that I've found limiting when writing a test suite.

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
Sometimes we want to access information about the state of a test. For example, when running UI tests, I like to take a screenshot on a test failure, so I can more easily see what went wrong. xUnit does not have a native way of determining if a test failed when you're in a tear down method. With TUnit, you can inject in a `TestContext` object into your tear down method, or you can call the static `TestContext.Current` static method.

### Assertions
xUnit assertions are fairly basic and have the problem of it being unclear which argument goes in which position, without sifting through intellisense/documentation.

```csharp
var one = 2;
Assert.Equal(1, one)
Assert.Equal(one, 1)
```

## NUnit

### Shared test class instances
This one has bitten me so many times, and I've seen it bite many others too. And a lot of people don't even know it. But the default behaviour of NUnit is to run all your tests within a class, against a single instance of that class. That means if you're storing state in fields/properties, they're going to be left over from previous tests.
This is what I call leaky test states, and I am firmly against it. Tests should be isolated from one another and really unable to affect one another. So TUnit by design runs every test against a new instance, and there is no way to change that because I consider it bad practice. If you want to share state in a field, then that's entirely possible by making it `static`. By utilising the language instead, it makes it clear to anyone reading it whether multiple tests can access that.

### Setting properties based off of dynamically injected data
I had a scenario in a multi-tenanted test suite where tests tests were repeated with different tenants injected in.
Like this:
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

With this, I wanted to be able to filter by the tenant. So I tried using a custom attribute with `IApplyToTest` and setting a property based on the constructor argument. This didn't work. I think they're enumerated upon starting, and so you can't set this up beforehand. With TUnit, tests are enumerated and initialised via source-generation so this is all done up-front. So I could set a property in TUnit with an attribute with `ITestDiscoveryEvent`, set a property based constructor arguments, and then run `dotnet run --treenode-filter /*/*/*/*[Tenant=MyTenant]` 

### Assembly & class level attributes
Want to use the `[Repeat]` or `[Retry]` attributes on a class? Or even an assembly? You can't. They're only supported for test methods. With TUnit, most attributes are supported at Test, Class & Assembly levels. Test takes the highest priority, then class, then assembly. So you could set defaults with an assembly/class attribute, and then override it for certain tests by setting that same attribute on the test.

### Assertions
NUnit assertions largely influenced the way that TUnit assertions work. However, NUnit assertions do not have compile time checks. I could check if a string is negative (`NUnitAssert.That("String", Is.Negative);`) or if a boolean throws an exception (`NUnitAssert.That(true, Throws.ArgumentException);`). These assertions don't make sense. There are analyzers to help catch these - But they will compile if these analyzers aren't run. TUnit assertions are built with the type system in mind (where possible!). Specific assertions are built via extensions to the relevant types, and not in a generic sense that could apply to anything. That means when you're using intellisense to see what methods you have available, you should only see assertions that are relevant for your type. This makes it harder to make mistakes, and decreases your feedback loop time.

## Other

### Source generated + Native AOT Support + Single File Support
As mentioned, TUnit is source generated. This should mean things are fast. And you can check out the generated code yourself! Because tests are source generated and not scanned via reflection, this means you can build your test projects using Native AOT or as a Single File application - Something that you can't current do with NUnit or xUnit.

### More lifecycle hooks
TUnit has tried to make it easy to hook into a range of lifecycles.
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

And all those hooks allow injecting in a relevant `[HookType]Context` object - So you can interrogate it for information about the test run so far. Hopefully meeting the needs of most users!


### Test dependencies
Got tests that require another test to execute first?
In other frameworks it usually involves turning off parallelisation, then setting an `[Order]` attribute with 1, 2, 3, etc.
In TUnit, you can use a `[DependsOn(...)]` attribute. That test will wait to start, only once its dependencies have finished. And you don't have to turn off parallelisation of other tests!

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
A lot of the data injection mechanisms in xUnit/NUnit work for the method, or the class, and not vice-versa. With TUnit, you can use `[Arguments(...)]` or `[Matrix(...)]` or `[MethodDataSource(...)]` etc. for both classes and test methods, making it super flexible!