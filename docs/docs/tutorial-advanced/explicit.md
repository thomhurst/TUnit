---
sidebar_position: 9
---

# Explicit

If you want a test to only be run explicitly (and not part of all general tests) then you can add the `[ExplicitAttribute]`.

This can be added to a test method or a test class.

A test is considered 'explicitly' run when all filtered tests have an explicit attribute on them. 

That means that you could run all tests in a class with an `[Explicit]` attribute. Or you could run a single method with an `[Explicit]` attribute. But if you try to run a mix of explicit and non-explicit tests, then the ones with an `[Explicit]` attribute will be excluded from the run.

This can be useful for 'Tests' that make sense in a local environment, and maybe not part of your CI builds. Or they could be helpers that ping things to warm them up, and by making them explicit tests, they are easily runnable, but don't affect your overall test suite.

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

