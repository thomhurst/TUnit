---
sidebar_position: 10
---

# Not in Parallel

By default, TUnit tests will run in parallel. 

To remove this behaviour, we can add a `[NotInParallel]` attribute to our test methods or classes.

This also takes an optional array of constraint keys.

If no constraint keys are supplied, then the test will only be run by itself.
If any constraint keys are set, the test will not be run alongside any other tests with any of those same keys. However it may still run in parallel alongside tests with other constraint keys.

For the example below, `MyTest` and `MyTest2` will not run in parallel with each other because of the common `DatabaseTest` constraint key, but `MyTest3` may run in parallel with the other two.

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    private const string DatabaseTest = "DatabaseTest";
    private const string RegistrationTest = "RegistrationTest";
    private const string ParallelTest = "ParallelTest";

    [Test]
    [NotInParallel(DatabaseTest)]
    public async Task MyTest()
    {
        
    }

    [Test]
    [NotInParallel(DatabaseTest, RegistrationTest)]
    public async Task MyTest2()
    {
        
    }

    [Test]
    [NotInParallel(ParallelTest)]
    public async Task MyTest3()
    {
        
    }
}
```
