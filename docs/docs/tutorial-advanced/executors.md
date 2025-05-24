---
sidebar_position: 12
---

# Executors

In some advanced cases, you may need to control how a test or hook is executed.
There are two interfaces that you can implement to control this:
- `IHookExecutor`
- `ITestExecutor`

You will be given a delegate, and some context about what is executing, and you can control how to invoke it.

To register your executor, on your test/hook, you can place an attribute of either:
- `[HookExecutor<...>]`
- `[TestExecutor<...>]`

An example of where you might need this is running in a STA Thread.

Here's an example:

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [TestExecutor<STAThreadExecutor>]
    public async Task With_STA()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEqualTo(ApartmentState.STA);
    }
    
    [Test]
    public async Task Without_STA()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEqualTo(ApartmentState.MTA);
    }
}
```
