using TUnit.Assertions.SourceGenerator.Tests.Options;

namespace TUnit.Assertions.SourceGenerator.Tests;

internal class CancellationTokenAssertionGeneratorTests : TestsBase
{
    [Test]
    public Task GeneratesCancellationTokenAssertions() => RunTest(
        Path.Combine(Git.RootDirectory.FullName,
            "TUnit.Assertions",
            "Conditions",
            "CancellationTokenAssertionExtensions.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().GreaterThanOrEqualTo(1);
        });
}
