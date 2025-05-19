namespace TUnit.Templates.Tests;

public class PlaywriteTemplateTests : TemplateTestBase
{
    protected override string TemplateShortName { get; set; }

    [Test]
    public async Task InstantiationTest()
    {
        TemplateShortName = "TUnit.Playwright";
        await Engine.Execute(Options).ConfigureAwait(false);
    }
}