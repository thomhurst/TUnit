---
sidebar_position: 11
---

# Parallel Groups

Parallel Groups are an alternative parallel mechanism to [NotInParallel].

Instead, classes that share a [ParallelGroup("")] attribute with the same key, may all run together in parallel, and nothing else will run alongside them.

For the example below, all `MyTestClass` tests may run in parallel, and all `MyTestClass2` tests may run in parallel. But they should not overlap and execute both classes at the same time.

```csharp
using TUnit.Core;

namespace MyTestProject;

[ParallelGroup("Group1")]
public class MyTestClass
{
    [Test]
    public async Task MyTest()
    {
        
    }

    [Test]
    public async Task MyTest2()
    {
        
    }

    [Test]
    public async Task MyTest3()
    {
        
    }
}

[ParallelGroup("Group2")]
public class MyTestClass2
{
    [Test]
    public async Task MyTest()
    {
        
    }

    [Test]
    public async Task MyTest2()
    {
        
    }

    [Test]
    public async Task MyTest3()
    {
        
    }
}
```