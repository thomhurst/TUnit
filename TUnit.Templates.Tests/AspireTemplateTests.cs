namespace TUnit.Templates.Tests
{
    public class AspireTemplateTests : TemplateTestBase
    {
        protected override string TemplateShortName { get; set; } = "TUnit.Aspire.Test";

        [Test]
        public async Task InstantiationTest()
        {
            await Engine.Execute(Options).ConfigureAwait(false);
        }
    }
}
