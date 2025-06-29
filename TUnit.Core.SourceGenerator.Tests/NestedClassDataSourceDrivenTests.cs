using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class NestedClassDataSourceDrivenTests : TestsBase
{
    [Test]
    public Task Properties() => DataPropertiesGenerator.RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "NestedClassDataSourceDrivenTests.cs"),
        new RunTestOptions
        {
            VerifyConfigurator = settingsTask => settingsTask.ScrubLinesContaining("PropertyInitializer_")
                .UniqueForTargetFrameworkAndVersion()
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(3);
        });

    [Test]
    public Task Test() => TestsGenerator.RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "NestedClassDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(3);
        });

    [Test]
    public Task Properties2() => DataPropertiesGenerator.RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "NestedClassDataSourceDrivenTests2.cs"),
        new RunTestOptions
        {
            VerifyConfigurator = settingsTask => settingsTask.ScrubLinesContaining("PropertyInitializer_")
                .UniqueForTargetFrameworkAndVersion(),
            AdditionalFiles =
            [
                Path.Combine(Sourcy.DotNet.Projects.TUnit_TestProject.DirectoryName!,
                    "Models",
                    "InitialisableClass.cs")
            ]
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(6);
        });

    [Test]
    public Task Test2() => TestsGenerator.RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "NestedClassDataSourceDrivenTests2.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
        });
}
