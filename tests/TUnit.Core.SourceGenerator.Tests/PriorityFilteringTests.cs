using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class PriorityFilteringTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "PriorityFilteringTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
                [
                    Path.Combine(Git.TestsDirectory.FullName,
                        "TUnit.TestProject",
                        "Enums",
                        "PriorityLevel.cs")
                ]
        },
        async generatedFiles =>
        {
            });
}
