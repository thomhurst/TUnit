---
sidebar_position: 10
---

# Not in Parallel

By default, TUnit tests will run in parallel. 

To remove this behaviour, we can add a  `[NotInParallelAttribute]` to our test methods or classes.

This also takes an optional array of constraint keys.

If no constraint keys are supplied, then the test will only be run by itself.
If any constraint keys are set, the test will not be run alongside any other tests with any of those same keys. However it may still run in parallel alongside tests with other constraint keys.

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [NotInParallel("DatabaseTest")]
    public async Task MyTest()
    {
        
    }

    [Test]
    [NotInParallel("DatabaseTest", "RegistrationTest")]
    public async Task MyTest2()
    {
        
    }
}
```