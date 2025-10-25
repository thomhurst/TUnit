using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

public class AotConverterGeneratorTests : TestsBase
{
    [Test]
    [Skip("Need to investigate - Behaves differently on local vs CI")]
    public Task GeneratesCode() => AotConverterGenerator.RunTest(
        Path.GetTempFileName(),
        new RunTestOptions
        {
            AdditionalFiles = Sourcy.DotNet.Projects.TUnit_TestProject.Directory!.EnumerateFiles("*.cs", SearchOption.AllDirectories).Select(x => x.FullName).ToArray()
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsGreaterThan(0);
        });
}
