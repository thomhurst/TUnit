# Display Names

If you want simple control over the name of a test, you can use the `[DisplayName(...)]` attribute.

```csharp
    [Test]
    [DisplayName("My first test!")]
    public async Task Test()
    {
        ...
    }
```

You can reference test parameters in the display name by using `$parameterName` within the attribute string. At runtime, these will be replaced with the actual argument values for each test case.

```csharp
    [Test]
    [Arguments("foo", 1, true)]
    [Arguments("bar", 2, false)]
    [DisplayName("Test with: $value1 $value2 $value3!")]
    public async Task Test3(string value1, int value2, bool value3)
    {
        ...
    }
```

The above would generate two test cases with their respective display name as:
- "Test with: foo 1 True"
- "Test with: bar 2 False"

If you have custom classes, you can combine this with [Argument Formatters](customization-extensibility/argument-formatters.md) to specify how to show them.

:::info
If you want to include a literal `$` in your display name, escape it as `$$`.  
For example: `[DisplayName("Total cost: $$100")]` will display as "Total cost: $100".
:::

## Custom Logic

If you want to have more control over how your test names are, you can create an attribute that inherits from `DisplayNameFormatterAttribute`.

There you will find a method that you must override: `FormatDisplayName`.
Here you have access to all the arguments and test details via the `TestContext` parameter.

Then simply add that custom attribute to your test.
