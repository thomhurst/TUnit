namespace TUnit.Templates.Tests;

public class AspNetTemplateTests : TemplateTestBase
{
    protected override string TemplateShortName { get; set; } = "TUnit.AspNet";

    [Test]
    public async Task InstantiationTest()
    {
        await Engine.Execute(Options).ConfigureAwait(false);
    }
}