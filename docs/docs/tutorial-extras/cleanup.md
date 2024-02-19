---
sidebar_position: 2
---

# Test Clean Ups

TUnit supports having your test class implement `IDisposable` or `IAsyncDisposable`. These will be called after your test has finished executing.

You can also  declare a method with a `[CleanUp]` or an `[OnlyOnceCleanUp]` attribute.

- `[CleanUp]` methods should NOT be static, and they will be executed repeatedly after each test in their class ends.
- `[OnlyOnceCleanUp]` methods SHOULD be static, and they will be executed only once, after all tests in their class end.

Methods will be executed top-down, so the current class clean ups will execute first, then the base classes.

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    private int _value;
    private static HttpResponseMessage? _pingResponse;

    [OnlyOnceCleanUp]
    public static async Task KillChromedrivers()
    {
        await Task.CompletedTask;

        foreach (var process in Process.GetProcessesByName("chromedriver.exe"))
        {
            process.Kill();
        }
    }
    
    [CleanUp]
    public async Task CleanUp()
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