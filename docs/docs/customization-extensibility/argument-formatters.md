# Argument Formatters

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

:::info
You can apply multiple `[ArgumentDisplayFormatter<T>]` attributes if you have different types to format.  
The first formatter whose `CanHandle` returns true will be used.
:::
