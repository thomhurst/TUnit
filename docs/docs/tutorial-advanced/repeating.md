---
sidebar_position: 4
---

# Repeating

If you want to repeat a test, add a `[RepeatAttribute]` onto your test method or class. This takes an `int` of how many times you'd like to repeat. Each repeat will show in the test explorer as a new test.

This can be used on base classes and inherited to affect all tests in sub-classes.

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [Repeat(3)]
    public async Task MyTest()
    {
        
    }
}
```