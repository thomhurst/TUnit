---
sidebar_position: 2
---

# Test Clean Ups

TUnit supports having your test class implement `IDisposable` or `IAsyncDisposable`. These will be called after your test has finished executing. However, using the attributes below offers better support for running multiple methods, and without having to implement your own try/catch logic. Every `[After]` method will be run, and any exceptions will be lazily thrown afterwards.

You can also declare a method with an `[After(...)]` or a `[GlobalAfter(...)]` attribute.

- `[After(EachTest)]` methods should NOT be static, and they will be executed repeatedly after each test in their class ends.
- `[After(Class)]` methods SHOULD be static, and they will be executed only once, after all tests in their class end.
- `[After(Assembly)]` methods SHOULD be static, and they will be executed only once, after all tests in their assembly end.


- All `[GlobalAfter(...)]` methods SHOULD be static, and they will follow the same behaviour as above, but fire for every test/class/assembly that is being run in the test session.

Methods will be executed top-down, so the current class clean ups will execute first, then the base classes' last.

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
    
    [After(EachTest)]
    public async Task AfterEachTest()
    {
        await new HttpClient().GetAsync($"https://localhost/test-finished-notifier?testName={TestContext.Current.TestInformation.TestName}");
    }

    [Test]
    public async Task Test()
    {
        // Do something
    }
}
```