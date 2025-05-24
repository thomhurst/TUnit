# Timeouts

If you want to stop a test after a specified amount of time, add a `[Timeout]` attribute onto your test method or class. This takes an `int` of how many milliseconds a test can execute for.

A cancellation token will be passed to tests too, which should be used where appropriate. This ensures that after the timeout is reached, operations are cancelled properly, and not wasting system resources.

This can be used on base classes and inherited to affect all tests in sub-classes.

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [Timeout(30_000)]
    public async Task MyTest(CancellationToken cancellationToken)
    {
        
    }
}
```
