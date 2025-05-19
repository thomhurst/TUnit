namespace TUnit.Templates.Tests
{
    public class AspireTemplateTests:TemplateTestBase
    {
        protected override string TemplateShortName { get; set; }

        [Test]
        public async Task InstantiationTest()
        {
            TemplateShortName = "TUnit.Aspire.Test";
            await Engine.Execute(Options).ConfigureAwait(false);
        }
    }
}
