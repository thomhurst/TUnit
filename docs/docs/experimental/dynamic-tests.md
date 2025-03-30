---
sidebar_position: 1
---

# Dynamically Created Tests

TUnit offers the ability to create your tests via dynamic code, as opposed to the standard `[Test]` attribute and data attributes.

To do this, create a new class with a public parameterless constructor.

Then, create a public method, called whatever you like, with a single parameter of `DynamicTestBuilderContext`, and place the `[DynamicTestBuilder]` attribute on that method.

On the `context` parameter, you can call `.AddTest(...)`, and pass it a `new DynamicTest<T>`.

`T` is the class containing the method you want to invoke.
If your class contains parameters or properties, or your method contains parameters, you can pass these into the test parameter

For simple tests, there is not much point using this feature. But it may be helpful when wanting to generate lots of test cases with different data.

Here's an example:

```csharp
namespace TUnit.TestProject.DynamicTests;

public class Basic
{
    public void SomeMethod()
    {
        Console.WriteLine(@"Hello, World!");
    }
    
    [DynamicTestBuilder]
    public void BuildTests(DynamicTestBuilderContext context)
    {
        context.AddTest(new DynamicTest<Basic>
        {
            TestMethod = @class => @class.SomeMethod(),
            TestMethodArguments = [],
            Attributes = [new RepeatAttribute(5)]
        });
    }
}
```
