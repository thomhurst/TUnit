namespace TUnit.Templates.Tests
{
    public class AspireTemplateTests:TemplateTestBase
    {
        protected override string TemplateShortName => "TUnit.Aspire";

        [Test]
        public async Task InstantiationTest()
        {
            await Engine.Execute(Options).ConfigureAwait(false);
        }
    }
}
