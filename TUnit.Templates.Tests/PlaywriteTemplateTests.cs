namespace TUnit.Templates.Tests;

public class PlaywriteTemplateTests : TemplateTestBase
{
    protected override string TemplateShortName => "TUnit.Playwright";

    [Test]
    public async Task InstantiationTest()
    {
        await Engine.Execute(Options).ConfigureAwait(false);
    }
}