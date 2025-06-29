using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class DisableReflectionScannerTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BasicTests.cs"),
        new RunTestOptions
        {
            VerifyConfigurator = verify =>
            {
                return verify.UniqueForTargetFrameworkAndVersion()
                    .ScrubLinesWithReplace(line =>
                    {
                        if (line.Contains("file static class DisableReflectionScanner_"))
                        {
                            return "file static class DisableReflectionScanner_Guid";
                        }

                        return line;
                    });
            }
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasSingleItem();
        });
}
