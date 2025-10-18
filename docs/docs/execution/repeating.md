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

## Global Repeat

In case you want to apply the repeat logic to all tests in a project, you can add the attribute on the assembly level.

```csharp
[assembly: Repeat(3)]
```

Or you can apply the repeat policy on all the tests in a class like this:

```csharp
[Repeat(3)]
public class MyTestClass
{
}
```

The more specific attribute will always override the more general one.
For example, the `[Repeat(3)]` on a method will override the `[Repeat(5)]` on the class,
which in turn will override the `[Repeat(7)]` on the assembly.

So the order of precedence is:
1. Method
1. Class
1. Assembly
