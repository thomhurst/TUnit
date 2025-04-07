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
    public void SomeMethod(string name)
    {
        Console.WriteLine(@$"Hello, {name}!");
    }
    
    [DynamicTestBuilder]
    public void BuildTests(DynamicTestBuilderContext context)
    {
        context.AddTest(new DynamicTest<Basic>
        {
            TestMethod = @class => @class.SomeMethod(DynamicTest.Argument<string>()),
            TestMethodArguments = [ "Tom" ],
            Attributes = [new RepeatAttribute(5)]
        });
    }
}
```

The test method body is used as an `Expression` - Not as a `delegate`. This means that arguments passed to it within the lambda will be ignored. And if the method is async, it does not need to be awaited.
Arguments must be provided via the `TestMethodArguments` property.

To make this clearer, it's recommended to use the `DynamicTest.Argument<T>()` helper.

It is also possible to build a test from within another test:

```csharp
[RunOnDiscovery]
[Arguments(1, 2, 3)]
[Arguments(101, 202, 303)]
public class Runtime(int a, int b, int c)
{
    public void SomeMethod(int arg1, int arg2, int arg3)
    {
        Console.WriteLine(@"SomeMethod called with:");
        Console.WriteLine($@"Class args: {a}, {b}, {c}");
        Console.WriteLine($@"Method args: {arg1}, {arg2}, {arg3}");
    }
    
    [Test]
    [Arguments(4, 5, 6)]
    [Arguments(404, 505, 606)]
    public async Task BuildTests(int d, int e, int f)
    {
        var context = TestContext.Current!;
        
        await context.AddDynamicTest(new DynamicTest<Runtime>
        {
            TestMethod = @class => @class.SomeMethod(0, 0, 0),
            TestClassArguments = [a + 10, b + 10, c + 10],
            TestMethodArguments = [d + 10, e + 10, f + 10],
            Attributes = [new RepeatAttribute(5)]
        });
    }
}
```