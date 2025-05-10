using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests.Bugs._1889;

internal class Tests1889 : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "1889",
            "DerivedTest.cs"),
        new RunTestOptions
        {
            VerifyConfigurator = settingsTask => settingsTask.ScrubLinesContaining("TestFilePath = ")
                .UniqueForTargetFrameworkAndVersion()
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
        });
}