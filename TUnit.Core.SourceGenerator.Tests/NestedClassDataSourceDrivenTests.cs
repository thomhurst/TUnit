using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class NestedClassDataSourceDrivenTests : TestsBase<DataGeneratorPropertyGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "NestedClassDataSourceDrivenTests.cs"),
        new RunTestOptions()
        {
            VerifyConfigurator = settingsTask => settingsTask.ScrubLinesContaining("PropertyInitializer_")
                .UniqueForTargetFrameworkAndVersion()
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(4);
        });
}
