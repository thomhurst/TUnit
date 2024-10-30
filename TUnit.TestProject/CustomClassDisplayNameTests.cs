using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core.Extensions;
using TUnit.Engine.Extensions;

#pragma warning disable CS9113 // Parameter is unread.

namespace TUnit.TestProject;

[ClassDataSource<Base1>]
[ClassDataSource<Base2>]
[ClassDataSource<Base3>]
[ArgumentDisplayFormatter<MyFormatter>]
public class CustomClassDisplayNameTests(Base1 base1)
{
    [Test]
    public async Task Test()
    {
        await Assert.That(TestContext.Current!.GetTestDisplayName()).IsEqualTo("A super important test!");
    }
}

public class MyFormatter : ArgumentDisplayFormatter
{
    public override bool CanHandle(object? value)
    {
        return value is Base1 or Base2 or Base3;
    }

    public override string FormatValue(object? value)
    {
        return value switch
        {
            Base3 base3 => "Third Base!",
            Base2 base2 => "Second Base!",
            Base1 base1 => "First Base!",
            _ => value?.ToString() ?? string.Empty
        };
    }
}