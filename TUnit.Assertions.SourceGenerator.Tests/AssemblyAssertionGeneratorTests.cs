using TUnit.Assertions.SourceGenerator.Tests.Options;

namespace TUnit.Assertions.SourceGenerator.Tests;

internal class AssemblyAssertionGeneratorTests : TestsBase
{
    [Test]
    public Task GeneratesAssemblyAssertions() => RunTest(
        Path.Combine(Git.RootDirectory.FullName,
            "TUnit.Assertions",
            "Conditions",
            "AssemblyAssertionExtensions.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).IsNotEmpty();
        });
}
