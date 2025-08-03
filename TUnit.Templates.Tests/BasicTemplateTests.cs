namespace TUnit.Templates.Tests;

public class BasicTemplateTests : TemplateTestBase
{
    protected override string TemplateShortName { get; set; } = "TUnit";

    [Test]
    public async Task InstantiationTest()
    {
        await Engine.Execute(Options).ConfigureAwait(false);
    }

    [Test]
    public async Task InstantiationTestWithFSharp()
    {
        TemplateShortName = "TUnit.FSharp";
        await Engine.Execute(Options).ConfigureAwait(false);
    }

    [Test]
    public async Task InstantiationTestWithVB()
    {
        TemplateShortName = "TUnit.VB";
        await Engine.Execute(Options).ConfigureAwait(false);
    }
}
