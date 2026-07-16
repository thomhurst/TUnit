namespace TUnit.Templates.Tests
{
    public class AspireStarterTemplateTests : TemplateTestBase
    {
        protected override string TemplateShortName { get; set; } = "TUnit.Aspire.Starter";

        [Test]
        public async Task InstantiationTest()
        {
            await Engine.Execute(Options).ConfigureAwait(false);
        }
    }
}
