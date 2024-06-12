---
sidebar_position: 9
---

# Explicit

If you want a test to only be run explicitly (and not part of all general tests) then you can add the `[ExplicitAttribute]`.

This can be added to a test method or a test class.

If added to a test method, then all tests generated from that test method (different test cases generated from different test data) will only be run if that method has been explicitly run.
If added to a test class, then all tests generated within that class will only be run if only tests within that class have been explicitly run.

A test is considered 'explicitly' run when:
- A test filter was used and matches your test
    e.g.  `dotnet run --treenode-filter "/*/*/*/*"`
- You ran your test from within the test explorer in your IDE specifically


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
```
