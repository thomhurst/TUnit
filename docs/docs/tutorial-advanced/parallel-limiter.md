---
sidebar_position: 11
---

# Parallel Limiter

TUnit allows the user to control the parallel limit on a test, class or assembly level.

To do this, we add a `[ParallelLimiter<>]` attribute.

You'll notice this has a generic type argument - You must give it a type that implements `IParallelLimit` and has a public empty constructor. That interface requires you to define what the limit is for those tests.

If a class doesn't have a parallel limit defined, it'll try and eagerly run when the .NET thread pool allows it to do so, so the upper limit is unknown.

If it does have a parallel limit defined, be aware that that parallel limit is shared for any tests with that same `Type` of parallel limit. 

In the example below, `MyParallelLimit` has a limit of `2`. Now any test, anywhere in your test suite, that has this parallel limit attribute applied to it, will shared this limit, and so only 2 can be processed at a time. 

Other tests without this attribute may run alongside them still. 

And other tests with a different `Type` of parallel limit may also run alongside them still, but limited amongst themselves by their shared `Type` and limit.

So be aware that limits are only shared among tests with that same `IParallelLimit` `Type`.

So if you wanted to do a global limit on an assembly, you could do:

```csharp
[assembly: ParallelLimiter<MyParallelLimit>]
```

And as long as that isn't overridden on a test or class, then that will apply to all tests in an assembly and be shared among them all, limiting how many run in parallel.

## Example

```csharp
using TUnit.Core;

namespace MyTestProject;

[ParallelLimiter<MyParallelLimit>]
public class MyTestClass
{
    [Test, Repeat(10)]
    public async Task MyTest()
    {
        
    }

    [Test, Repeat(10)]
    public async Task MyTest2()
    {
        
    }
}

public record MyParallelLimit : IParallelLimit
{
    public int Limit => 2;
}
```

## Caveats
If a test uses `[DependsOn(nameof(OtherTest))]` and the other test has its own different parallel limit, this isn't guaranteed to be honoured.
