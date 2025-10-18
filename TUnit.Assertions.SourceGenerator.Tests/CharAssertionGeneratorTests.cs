using TUnit.Assertions.SourceGenerator.Tests.Options;

namespace TUnit.Assertions.SourceGenerator.Tests;

internal class CharAssertionGeneratorTests : TestsBase
{
    [Test]
    public Task GeneratesCharAssertions() => RunTest(
        Path.Combine(Git.RootDirectory.FullName,
            "TUnit.Assertions",
            "Conditions",
            "CharAssertionExtensions.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).IsNotEmpty();
        });
}
