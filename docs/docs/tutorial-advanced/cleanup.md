---
sidebar_position: 2
---

# Test Clean Ups

TUnit supports having your test class implement `IDisposable` or `IAsyncDisposable`. These will be called after your test has finished executing. However, using the attributes below offers better support for running multiple methods, and without having to implement your own try/catch logic. Every `[After]` method will be run, and any exceptions will be lazily thrown afterwards.

You can also declare a method with an `[After(...)]` or an `[AfterEvery(...)]` attribute.

## [After(HookType)]

### [After(Test)]
Must be an instance method. Will be executed after each test in the class it's defined in.
Methods will be executed top-down, so the current class clean ups will execute first, then the base classes' last.

### [After(Class)]
Must be a static method. Will run once after the last test in the class it's defined it finishes.

### [After(Assembly)]
Must be a static method. Will run once after the last test in the assembly it's defined it finishes.

### [After(TestSession)]
Must be a static method. Will run once after the last test in the test session finishes.

### [After(TestDiscovery)]
Must be a static method. Will run once after tests are discovered.

## [AfterEvery(HookType)]
All [AfterEvery(...)] methods must be static - And should ideally be placed in their own file that's easy to find, as they can globally affect the test suite, so it should be easy for developers to locate this behaviour.
e.g. `GlobalHooks.cs` at the root of the test project.

### [AfterEvery(Test)]
Will be executed after every test that will run in the test session.

### [AfterEvery(Class)]
Will be executed after the last test of every class that will run in the test session.

### [AfterEvery(Assembly)]
Will be executed after the last test of every assembly that will run in the test session.

### [AfterEvery(TestSession)]
The same as [After(TestSession)]

### [AfterEvery(TestDiscovery)]
The same as [After(TestDiscovery)]

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    private int _value;
    private static HttpResponseMessage? _pingResponse;

    [After(Class)]
    public static async Task KillChromedrivers()
    {
        await Task.CompletedTask;

        foreach (var process in Process.GetProcessesByName("chromedriver.exe"))
        {
            process.Kill();
        }
    }
    
    [After(Test)]
    public async Task AfterEachTest()
    {
        await new HttpClient().GetAsync($"https://localhost/test-finished-notifier?testName={TestContext.Current.TestInformation.TestName}");
    }

    [Test]
    public async Task MyTest()
    {
        // Do something
    }
}
```
