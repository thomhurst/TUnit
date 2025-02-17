namespace TUnit.Templates.Tests;

public class BasicTemplateTests : TemplateTestBase
{
    protected override string TemplateShortName => "TUnit";

    [Test]
    public async Task InstantiationTest()
    {
        await Engine.Execute(Options).ConfigureAwait(false);
    }
}