namespace TUnit.Templates.Tests
{
    public class AspireStarterTemplateTests : TemplateTestBase
    {
        protected override string TemplateShortName { get; set; }

        [Test]
        public async Task InstantiationTest()
        {
            TemplateShortName = "TUnit.Aspire.Starter";
            await Engine.Execute(Options).ConfigureAwait(false);
        }
    }
}