using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests.Bugs._Issue2887;

internal class Issue2887Tests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "Issue2887",
            "ReproTest.cs"),
        new RunTestOptions
        {
            VerifyConfigurator = settingsTask => settingsTask.ScrubLinesContaining("TestFilePath = ")
        },
        async generatedFiles =>
        {
            // This test ensures that abstract classes with parameterized constructors and hooks don't cause generation errors
        });
}