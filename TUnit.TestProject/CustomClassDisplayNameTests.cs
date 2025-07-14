using TUnit.TestProject.AfterTests;
using TUnit.TestProject.Attributes;

#pragma warning disable CS9113 // Parameter is unread.

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<Base1>]
[ClassDataSource<Base2>]
[ClassDataSource<Base3>]
[ArgumentDisplayFormatter<MyFormatter>]
public class CustomClassDisplayNameTests(Base1 base1)
{
    [Test]
    public async Task Test()
    {
        await Assert.That(TestContext.Current!.GetDisplayName())
            .IsEqualTo("Test(First Base!)")
            .Or
            .IsEqualTo("Test(Second Base!)")
            .Or
            .IsEqualTo("Test(Third Base!)");
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
            Base3 => "Third Base!",
            Base2 => "Second Base!",
            Base1 => "First Base!",
            _ => value?.ToString() ?? string.Empty
        };
    }
}
