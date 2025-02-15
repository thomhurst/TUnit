namespace TUnit.Templates.Tests;

public class AspNetTemplateTests : TemplateTestBase
{
    protected override string TemplateShortName => "TUnit.AspNet";

    [Test]
    public async Task InstantiationTest()
    {
        await Engine.Execute(Options).ConfigureAwait(false);
    }
}