---
sidebar_position: 11
---

# Ordering Tests

:::warning

It is recommended to use [DependsOn(...)] as it provides more flexibility and doesnt sacrifice parallelisation.

:::

By default, TUnit tests will run in parallel. This means there is no order and it doesn't make sense to be able to control that.

However, if tests aren't running in parallel, they can absolutely be ordered, and this is necessary for some systems.

To control ordering, there is an `Order` property on the `[NotInParallel]` attribute.

Orders will execute from smallest to largest. So 1 first, then 2, then 3, etc.

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [NotInParallel(Order = 1)]
    public async Task MyTest()
    {
        
    }

    [Test]
    [NotInParallel(Order = 2)]
    public async Task MyTest2()
    {
        
    }
}
```
