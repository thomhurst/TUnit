using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests.Bugs._1889;

internal class Tests1889 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "1889",
            "DerivedTest.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName, "TUnit.TestProject.Library", "Bugs", "1889", "BaseTests.cs")
            ],
            VerifyConfigurator = settingsTask => settingsTask.ScrubLinesContaining("TestFilePath = ")
                .UniqueForTargetFrameworkAndVersion()
        },
        async generatedFiles =>
        {
            });
}
