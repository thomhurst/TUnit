using TUnit.Assertions.SourceGenerator.Tests.Options;

namespace TUnit.Assertions.SourceGenerator.Tests;

internal class TaskAssertionGeneratorTests : TestsBase
{
    [Test]
    public Task GeneratesTaskAssertions() => RunTest(
        Path.Combine(Git.RootDirectory.FullName,
            "TUnit.Assertions",
            "Conditions",
            "TaskAssertionExtensions.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).IsNotEmpty();
        });
}
