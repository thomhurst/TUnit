using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class InheritedTestsFromDifferentProjectTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "InheritedTestsFromDifferentProjectTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject.Library",
                    "BaseTests.cs"),
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "TestData.cs")
            ]
        },
        async generatedFiles =>
        {
            });
}
