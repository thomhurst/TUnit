using TUnit.Assertions.SourceGenerator.Generators;
using TUnit.Assertions.SourceGenerator.Tests.Options;

namespace TUnit.Assertions.SourceGenerator.Tests;

internal class DateOnlyAssertionGeneratorTests : TestsBase<MethodAssertionGenerator>
{
    [Test]
    public Task GeneratesDateOnlyAssertions() => RunTest(
        Path.Combine(Git.RootDirectory.FullName,
            "TUnit.Assertions",
            "Conditions",
            "DateOnlyAssertionExtensions.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().EqualTo(0);
        });
}
