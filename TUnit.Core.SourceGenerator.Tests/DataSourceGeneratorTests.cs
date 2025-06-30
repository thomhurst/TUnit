using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class DataSourceGeneratorTests : TestsBase
{
    [Test]
    public Task Typed() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "DataSourceGeneratorTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
        });

    [Test]
    public Task Untyped() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AutoDataTests.cs"),
        new RunTestOptions()
        {
            AdditionalFiles = [
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "Attributes",
                    "AutoDataAttribute.cs")
            ]
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
        });
}
