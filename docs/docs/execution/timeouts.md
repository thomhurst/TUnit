---
sidebar_position: 5
---

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

## Global Timeout

In case you want to apply the timeout to all tests in a project, you can add the attribute on the assembly level.

```csharp
[assembly: Timeout(3000)]
```

Or you can apply the Timeout on all the tests in a class like this:

```csharp
[Timeout(3000)]
public class MyTestClass
{
}
```

The more specific attribute will always override the more general one.
For example, the `[Timeout(3000)]` on a method will override the `[Timeout(5000)]` on the class,
which in turn will override the `[Timeout(7000)]` on the assembly.

So the order of precedence is:
1. Method
1. Class
1. Assembly
