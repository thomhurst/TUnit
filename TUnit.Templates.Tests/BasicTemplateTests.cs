using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace TUnit.Templates.Tests;

public class BasicTemplateTests
{
    private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    private const string templateShortName = "TUnit";

    [Test]
    public async Task InstantiationTest()
    {
        TemplateVerifierOptions options = new(templateName: templateShortName)
        {
            TemplatePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "content", "TUnit"),
        };

        VerificationEngine engine = new(_loggerFactory);
        await engine.Execute(options).ConfigureAwait(false);
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        _loggerFactory.Dispose();
    }
}