using TUnit.Assertions.SourceGenerator.Tests.Options;

namespace TUnit.Assertions.SourceGenerator.Tests;

internal class DirectoryInfoAssertionGeneratorTests : TestsBase
{
    [Test]
    public Task GeneratesDirectoryInfoAssertions() => RunTest(
        Path.Combine(Git.RootDirectory.FullName,
            "TUnit.Assertions",
            "Conditions",
            "DirectoryInfoAssertionExtensions.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().GreaterThanOrEqualTo(1);
        });
}
