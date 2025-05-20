namespace TUnit.Templates.Tests;

public class AspNetTemplateTests : TemplateTestBase
{
    protected override string TemplateShortName { get; set; }

    [Test]
    public async Task InstantiationTest()
    {
        TemplateShortName = "TUnit.AspNet";
        await Engine.Execute(Options).ConfigureAwait(false);
    }
}