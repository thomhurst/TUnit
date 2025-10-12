using TUnit.Assertions.SourceGenerator.Tests.Options;

namespace TUnit.Assertions.SourceGenerator.Tests;

internal class StreamAssertionGeneratorTests : TestsBase
{
    [Test]
    public Task GeneratesStreamAssertions() => RunTest(
        Path.Combine(Git.RootDirectory.FullName,
            "TUnit.Assertions",
            "Conditions",
            "StreamAssertionExtensions.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().GreaterThanOrEqualTo(1);
        });
}
