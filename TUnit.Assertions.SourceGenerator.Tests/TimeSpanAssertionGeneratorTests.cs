using TUnit.Assertions.SourceGenerator.Generators;
using TUnit.Assertions.SourceGenerator.Tests.Options;

namespace TUnit.Assertions.SourceGenerator.Tests;

internal class TimeSpanAssertionGeneratorTests : TestsBase<MethodAssertionGenerator>
{
    [Test]
    public Task GeneratesTimeSpanAssertions() => RunTest(
        Path.Combine(Git.RootDirectory.FullName,
            "TUnit.Assertions",
            "Conditions",
            "TimeSpanAssertionExtensions.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).IsNotEmpty();
        });
}
