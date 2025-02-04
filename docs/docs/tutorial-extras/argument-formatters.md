---
sidebar_position: 15
---

# Argument Formatters

If you are writing data driven tests, and using custom classes to represent your data, then the test explorer might not show you useful information to distinguish test cases, and instead only show you the class name.

If you want control over how injected arguments appear in the test explorer, you can create a class that inherits from `ArgumentDisplayFormatter` and then decorate your test with the `[ArgumentDisplayFormatter<T>]` attribute.

For example:

```csharp
    [Test]
    [MethodDataSource(nameof(SomeMethod))]
    [ArgumentDisplayFormatter<SomeClassFormatter>]
    public async Task Test(SomeClass)
    {
        await Assert.That(TestContext.Current!.GetTestDisplayName()).IsEqualTo("A super important test!");
    }
```

```csharp
public class MyFormatter : ArgumentDisplayFormatter
{
    public override bool CanHandle(object? value)
    {
        return value is SomeClass;
    }

    public override string FormatValue(object? value)
    {
        var someClass = (SomeClass)value;
        return $"One: {someClass.One} | Two: {someClass.Two}";
    }
}
```