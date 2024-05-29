<!-- ---
sidebar_position: 9
---

# Explicit

If you want a test to only be run explicitly (and not part of all general tests) then you can add the `[ExplicitAttribute]`.

This can be added to a test method or a test class.

If added to a test method, then all tests generated from that test method (different test cases generated from different test data) will only be run if that method has been explicitly run.
If added to a test class, then all tests generated within that class will only be run if only tests within that class have been explicitly run.

To explicitly run tests, you can either:
- Run `dotnet test --filter "SomeFilter=This" - Where the filter will match your explicit tests
- Run ONLY your explicit tests from within the test explorer. If you run other tests alongside your explicit tests, then your explicit tests will not run.


```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [Explicit]
    public async Task MyTest()
    {
        
    }
}
``` -->