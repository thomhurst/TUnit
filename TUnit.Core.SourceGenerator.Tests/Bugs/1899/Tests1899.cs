using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests.Bugs._1899;

internal class Tests1899 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "1899",
            "DerivedTest.cs"),
        new RunTestOptions
        {
            VerifyConfigurator = settingsTask => settingsTask.ScrubLinesContaining("TestFilePath = ")
        },
        async generatedFiles =>
        {
            });

    [Test]
    public Task BaseClass() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject.Library",
            "Bugs",
            "1899",
            "BaseClass.cs"),
        new RunTestOptions
        {
            VerifyConfigurator = settingsTask => settingsTask.ScrubLinesContaining("TestFilePath = ")
        },
        async generatedFiles =>
        {
            });
}
