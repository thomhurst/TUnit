namespace TUnit.Templates.Tests
{
    public class AspireStarterTemplateTests : TemplateTestBase
    {
        protected override string TemplateShortName => "TUnit.AspireStarter";

        [Test]
        public async Task InstantiationTest()
        {
            await Engine.Execute(Options).ConfigureAwait(false);
        }
    }
}