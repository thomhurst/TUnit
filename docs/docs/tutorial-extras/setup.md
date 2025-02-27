---
sidebar_position: 1
---

# Test Set Ups

Most setup for a test can be performed in the constructor (think setting up mocks, assigning fields.)

However some scenarios require further setup that could be an asynchronous operation.
E.g. pinging a service to wake it up in preparation for the tests.

For this, we can declare a method with a `[Before(...)]` or a `[BeforeEvery(...)]` attribute.

## [Before(HookType)]

### [Before(Test)]
Must be an instance method. Will be executed before each test in the class it's defined in.
Methods will be executed bottom-up, so the base class set ups will execute first and then the inheriting classes.

### [Before(Class)]
Must be a static method. Will run once before the first test in the class it's defined in starts.

### [Before(Assembly)]
Must be a static method. Will run once before the first test in the assembly it's defined in starts.

### [Before(TestSession)]
Must be a static method. Will run once before the first test in the test session starts.

### [Before(TestDiscovery)]
Must be a static method. Will run once before any tests are discovered.

## [BeforeEvery(HookType)]
All [BeforeEvery(...)] methods must be static - And should ideally be placed in their own file that's easy to find, as they can globally affect the test suite, so it should be easy for developers to locate this behaviour.
e.g. `GlobalHooks.cs` at the root of the test project.

### [BeforeEvery(Test)]
Will be executed before every test that will run in the test session.

### [BeforeEvery(Class)]
Will be executed before the first test of every class that will run in the test session.

### [BeforeEvery(Assembly)]
Will be executed before the first test of every assembly that will run in the test session.

### [BeforeEvery(TestSession)]
The same as [Before(TestSession)]

### [BeforeEvery(TestDiscovery)]
The same as [Before(TestDiscovery)]

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    private int _value;
    private static HttpResponseMessage? _pingResponse;

    [Before(Class)]
    public static async Task Ping()
    {
        _pingResponse = await new HttpClient().GetAsync("https://localhost/ping");
    }
    
    [Before(Test)]
    public async Task Setup()
    {
        await Task.CompletedTask;
        
        _value = 99;
    }

    [Test]
    public async Task MyTest()
    {
        await Assert.That(_value).IsEqualTo(99);
        await Assert.That(_pingResponse?.StatusCode)
            .IsNotNull()
            .And.IsEqualTo(HttpStatusCode.OK);
    }
}
```
## AsyncLocal

If you are wanting to set AsyncLocal values within your `[Before(...)]` hooks, this is supported.

But to propagate the values into the test framework, you must call `context.FlowAsyncLocalValues()` - Where `context` is the relevant context object injected into your hook method.

E.g.

```csharp
    [BeforeEvery(Class)]
    public static void BeforeClass(ClassHookContext context)
    {
        _myAsyncLocal.Value = "Some Value";
        context.FlowAsyncLocalValues();
    }
```